export interface UserProfileDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  userName: string
  email: string
}

export interface UserCreateDto {
  firstName: string
  lastName: string
  userName: string
  email: string
  password: string
}

export interface UserUpdateDto {
  id: string
  firstName: string
  lastName: string
  userName: string
  email: string
}

export interface UserUpdateSelfDto {
  firstName: string
  lastName: string
  userName: string
}

export interface ChangePasswordDto {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}
