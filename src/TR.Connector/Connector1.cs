using TR.Connectors.Api.Entities;

namespace TR.Connector
{
    public partial class Connector
    {
        class BaseResponse
        {
            public bool success { get; set; }
            public string errorText { get; set; }
        }
        
        //-------TokenResponse------------//
        class TokenResponseData
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }

        class TokenResponse: BaseResponse
        {
            public TokenResponseData data { get; set; }
            // public bool success { get; set; }
            // public object errorText { get; set; }
            public object count { get; set; }
        }
        //-------TokenResponse------------//

        //-------RoleResponse------------//
        class RoleResponseData
        {
            public int id { get; set; }
            public string name { get; set; }
            public string corporatePhoneNumber { get; set; }

            public string FormStringPermission(ERoleType role)
            {
                switch (role)
                {
                    case ERoleType.ItRole:
                        return $"ItRole,{id}";
                    case ERoleType.RequestRight:
                        return $"RequestRight,{id}";
                    default:
                        return null;
                }
            }
            
            
            public Permission FormPermission(ERoleType role)
            {
                switch (role)
                {
                    case ERoleType.ItRole:
                        return new Permission($"ItRole,{id}", name, corporatePhoneNumber);
                    case ERoleType.RequestRight:
                        return new Permission($"RequestRight,{id}", name, corporatePhoneNumber);
                    default:
                        return null;
                }   
            } 
        }

        class RoleResponse: BaseResponse
        {
            public List<RoleResponseData> data { get; set; }
            public int count { get; set; }
        }
        //-------RoleResponse------------//

        //-------RightResponse------------//
        class RightResponseData
        {
            public int id { get; set; }
            public string name { get; set; }
            public object users { get; set; }
        }

        class RightResponse: BaseResponse
        {
            public List<RightResponseData> data { get; set; }
            public int count { get; set; }
        }
        //-------RightResponse------------//


        //-------UserRoleResponse------------//
        class UserRoleResponse: BaseResponse
        {
            public List<RoleResponseData> data { get; set; }
            public int count { get; set; }
        }
        //-------UserRoleResponse------------//

        //-------UserRoleResponse------------//
        class UserrightResponse: BaseResponse
        {
            public List<RightResponseData> data { get; set; }
            public int count { get; set; }
        }
        //-------UserRoleResponse------------//


        //-------UserResponse------------//
        class UserResponseData
        {
            public string login { get; set; }
            public string status { get; set; }
        }

        class UserResponse: BaseResponse
        {
            public List<UserResponseData> data { get; set; }
            public int count { get; set; }
        }
        //-------UserResponse------------//

        //-------UserPropertyResponse------------//
        class UserPropertyData
        {
            public string lastName { get; set; }
            public string firstName { get; set; }
            public string middleName { get; set; }
            public string telephoneNumber { get; set; }
            public bool isLead { get; set; }
            public string login { get; set; }
            public string status { get; set; }
            
            public IEnumerable<Property> FormProperties()
            {
                yield return new Property(nameof(lastName), nameof(lastName));
                yield return new Property(nameof(firstName), nameof(firstName));
                yield return new Property(nameof(middleName), nameof(middleName));
                yield return new Property(nameof(telephoneNumber), nameof(telephoneNumber));
                yield return new Property(nameof(isLead), nameof(isLead));
                yield return new Property(nameof(status), nameof(status));
            }
            
            public IEnumerable<UserProperty> FormUserProperties()
            {
                yield return new UserProperty(nameof(lastName), lastName.ToString());
                yield return new UserProperty(nameof(firstName), firstName.ToString());
                yield return new UserProperty(nameof(middleName), middleName.ToString());
                yield return new UserProperty(nameof(telephoneNumber), telephoneNumber.ToString());
                yield return new UserProperty(nameof(isLead), isLead.ToString());
                yield return new UserProperty(nameof(status), status.ToString());
            }
            
            public void ChangeProperty(UserProperty property)
            {
                switch (property.Name)
                {
                    case nameof(lastName): lastName = property.Value; break; //lastName.
                    case nameof(firstName): firstName = property.Value; break; //firstName.
                    case nameof(middleName): middleName = property.Value; break; //middleName.
                    case nameof(telephoneNumber): telephoneNumber = property.Value; break; //telephoneNumber.
                    case nameof(isLead): isLead = Convert.ToBoolean(property.Value); break; //isLead.
                    case nameof(status): status = property.Value; break; //status.
                }
            }
        }

        class UserPropertyResponse: BaseResponse
        {
            public UserPropertyData data { get; set; }
            public int count { get; set; }
        }

        class CreateUSerDTO : UserPropertyData
        {
            public string password { get; set; }
        }
        
        class TryLoginDTO
        {
            public string login { get; set; }
            public string password { get; set; }
        }
        
        class AvoidPermissionsDTO
        {
        }
        //-------UserPropertyResponse------------//
    }
}
