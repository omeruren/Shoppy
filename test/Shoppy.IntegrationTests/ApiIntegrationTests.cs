using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Shoppy.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;


    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // There is no public self-registration endpoint — POST /api/v1/users requires the
    // Users.Create permission — so test users are seeded directly through UserManager
    // and granted the Admin role, then a real token is obtained via the login endpoint.
    private async Task<string> GetAuthenticatedTokenAsync(string username, string password)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByNameAsync(username);

            if (user is null)
            {
                user = User.Create("John", "Doe", username, $"{username}@example.com");
                var createResult = await userManager.CreateAsync(user, password);
                createResult.Succeeded.Should().BeTrue();
            }

            var adminRole = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == "Admin");

            if (adminRole is null)
            {
                adminRole = new Role { Name = "Admin" };
                context.AppRoles.Add(adminRole);
                await context.SaveChangesAsync();
            }

            var hasRole = await context.AppUserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);

            if (!hasRole)
            {
                context.AppUserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
                await context.SaveChangesAsync();
            }
        }

        // Login

        var loginDto = new LoginRequestDto(username, password);

        var logResponseDto = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        logResponseDto.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await logResponseDto.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.IsSuccessful.Should().BeTrue();

        return result.Data!.AccessToken;
    }

    [Fact]
    public async Task GetCategories_Should_Require_Authentication()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Full_Flow_Should_Create_And_Retrive_Categories()
    {
        // Arrange
        var token = await GetAuthenticatedTokenAsync("flowuser", "Password123!");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new CategoryCreateDto("Electronics Integration"));

        // Act - Create Category
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Get Categories
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/categories?pageNumber=1&pageSize=5");

        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var getResponse = await _client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await getResponse.Content.ReadFromJsonAsync<Result<PaginationResultDto<CategoryResultDto>>>();

        result.Should().NotBeNull();
        result!.IsSuccessful.Should().BeTrue();
        result.Data!.Data.Should().Contain(c => c.Name == "Electronics Integration");
    }

    [Fact]
    public async Task UpdateCategory_Should_Fail_With_409_Conflict_When_Concurrency_Violation_Occurs()
    {
        var token = await GetAuthenticatedTokenAsync("conflictuser", "Password123!");

        // 1. Create Category
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories");

        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        createRequest.Content = JsonContent.Create(new CategoryCreateDto("Concurrency Test Category"));

        var createResponse = await _client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);


        // Fetch the Category from DB to get its Id and RowVersion
        Guid categoryId;

        byte[] rowVersion;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var category = await context.Categories.FirstAsync(c => c.Name == "Concurrency Test Category");
            categoryId = category.Id;
            rowVersion = category.RowVersion;
        }

        // 2. Perform a successful update (this will increment/change the RowVersion in DB)
        var updateRequestA = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/categories/{categoryId}");

        updateRequestA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        updateRequestA.Content = JsonContent.Create(new CategoryUpdateDto(categoryId, "Updated By User A", rowVersion));

        var updateResponseA = await _client.SendAsync(updateRequestA);

        updateResponseA.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Perform a second update using the OLD RowVersion (should result in 409 Conflict because RowVersion changed)
        var updateRequestB = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/categories/{categoryId}");

        updateRequestB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        updateRequestB.Content = JsonContent.Create(new CategoryUpdateDto(categoryId, "Updated By User B", rowVersion));

        var updateResponseB = await _client.SendAsync(updateRequestB);

        // Assert - Concurrency exception correctly returns HTTP 409 Conflict

        updateResponseB.StatusCode.Should().Be(HttpStatusCode.Conflict);

    }

    [Fact]
    public async Task CreateProduct_Should_Return_409_When_Two_Concurrent_Requests_Use_Same_Name()
    {
        var token = await GetAuthenticatedTokenAsync("productconcurrencyuser", "Password123!");

        // Create a category for the products to reference
        var categoryRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories");
        categoryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        categoryRequest.Content = JsonContent.Create(new CategoryCreateDto("Concurrency Product Category"));

        var categoryResponse = await _client.SendAsync(categoryRequest);
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        Guid categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var category = await context.Categories.FirstAsync(c => c.Name == "Concurrency Product Category");
            categoryId = category.Id;
        }

        var productName = $"Concurrent Product {Guid.NewGuid()}";

        Task<HttpResponseMessage> CreateProductAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/products");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new ProductCreateDto(productName, null, 9.99m, categoryId));
            return _client.SendAsync(request);
        }

        // Act - fire two requests with the identical Name at (near-)the same time
        var responses = await Task.WhenAll(CreateProductAsync(), CreateProductAsync());

        // Assert - exactly one succeeds, the other is a 409 conflict (never an unhandled 500)
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
    }
}
