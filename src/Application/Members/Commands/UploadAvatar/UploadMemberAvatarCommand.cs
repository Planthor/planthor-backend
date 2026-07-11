using Application.Shared;

namespace Application.Members.Commands.UploadAvatar;

/// <summary>
/// Command to request the generation of a presigned URL for uploading a member's avatar.
/// </summary>
public record UploadMemberAvatarCommand : ICommand<string>;
