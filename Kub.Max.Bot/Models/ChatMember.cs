using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Участник чата.

public class ChatMember : UserWithPhoto
{
    [JsonPropertyName("last_access_time")]
    public long LastAccessTime { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("join_time")]
    public long JoinTime { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}


/// Права администратора чата.

public static class ChatAdminPermission
{
    public const string ReadAllMessages = "read_all_messages";
    public const string AddRemoveMembers = "add_remove_members";
    public const string AddAdmins = "add_admins";
    public const string ChangeChatInfo = "change_chat_info";
    public const string PinMessage = "pin_message";
    public const string Write = "write";
    public const string EditLink = "edit_link";
    public const string CanCall = "can_call";
    public const string PostEditDeleteMessage = "post_edit_delete_message";
    public const string EditMessage = "edit_message";
    public const string DeleteMessage = "delete_message";
}