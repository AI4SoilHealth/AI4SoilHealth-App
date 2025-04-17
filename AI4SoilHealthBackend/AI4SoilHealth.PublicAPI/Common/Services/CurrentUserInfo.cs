using System;

namespace PublicAPI.Common.Services;

/**
    * Current user info
    @module CurrentUserInfo
*/
/**
    * Current user info
    @class CurrentUserInfo
*/
public class CurrentUserInfo {
    public int LangId { get; set; }
    public int? PersonId { get; set; }
    public int? EmulatedPersonId { get; set; }

    public bool IsAdmin { get ; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
    public string Nickname { get; set; }
}
