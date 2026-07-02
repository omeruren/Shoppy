using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.DataAccess.Context;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Shoppy.IntegrationTests;

public class ApiIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;


    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private async Task<string> GetAuthenticatedTokenAsync(string username, string password)
    {
        // Register the user

        var registerDto = new UserCreateDto("John", "Doe", username, $"{username}@example.com", password);

        var regResponseDto = await _client.PostAsJsonAsync("/api/v1/users", registerDto);

        regResponseDto.StatusCode.Should().Match(s => s == HttpStatusCode.Created || s == HttpStatusCode.Conflict || s == HttpStatusCode.OK);

        // Seed Admin Role in Db if not exists

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminRole = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == "Admin");

            if (adminRole is null)
            {
                adminRole = new Entity.Models.Role { Name = "Admin" };
                context.AppRoles.Add(adminRole);
                await context.SaveChangesAsync();
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (username != null)
            {
                var hasRole = await context.AppUserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);

                if (!hasRole)
                {
                    context.AppUserRoles.Add(new Entity.Models.UserRole { UserId = user.Id, RoleId = adminRole.Id });
                }
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
        var updateRequestA = new HttpRequestMessage(HttpMethod.Put, "/api/v1/categories");

        updateRequestA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        updateRequestA.Content = JsonContent.Create(new CategoryUpdateDto(categoryId, "Updated By User A", rowVersion));

        var updateResponseA = await _client.SendAsync(updateRequestA);

        updateResponseA.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Perform a second update using the OLD RowVersion (should result in 409 Conflict because RowVersion changed)
        var updateRequestB = new HttpRequestMessage(HttpMethod.Put, "/api/v1/categories");

        updateRequestB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        updateRequestB.Content = JsonContent.Create(new CategoryUpdateDto(categoryId, "Updated By User B", rowVersion));

        var updateResponseB = await _client.SendAsync(updateRequestB);

        // Assert - Concurrency exception correctly returns HTTP 409 Conflict

        updateResponseB.StatusCode.Should().Be(HttpStatusCode.Conflict);

    }
}
