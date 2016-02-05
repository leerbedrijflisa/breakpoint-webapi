using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    public class ErrorMessages
    {
        public static string Get(int code, dynamic obj)
        {
            string message = string.Empty;
            switch (code)
            {
                case 0:
                    message = obj.Message;
                    break;

                // 11XX CODES
                case 1100:
                    message = string.Format("The body is empty.");
                    break;
                case 1101:
                    message = string.Format("Field '{0}' is required.", obj.Field);
                    break;
                case 1102:
                    message = string.Format("Subfield '{0}' is required in the {1} named '{2}'.", obj.SubField, obj.Type, obj.Field);
                    break;
                case 1103:
                    message = string.Format("Field '{0}' is not allowed in the request body.", obj.Field);
                    break;
                case 1104:
                    message = string.Format("The {0} provided already exists, or shares it's {1} with another {0}.", obj.Type, obj.Value);
                    break;


                // 12XX CODES
                case 1200:
                    message = string.Format("The field '{0}' with value '{1}' can only contain letters.", obj.Field, obj.Value);
                    break;
                case 1201:
                    message = string.Format("The field '{0}' with value '{1}' can only contain letters and spaces.", obj.Field, obj.Value);
                    break;
                case 1202:
                    message = string.Format("The field '{0}' with value '{1}' can only contain numbers.", obj.Field, obj.Value);
                    break;
                case 1203:
                    message = string.Format("The field '{0}' with value '{1}' doesn't meet the requirements of {2} digit number.", obj.Field, obj.Value, obj.Count);
                    break;
                case 1204:
                    message = string.Format("The field '{0}' with value '{1}' can only contain {2}, {3} or {4}.", obj.Field, obj.Value, obj.Permitted1, obj.Permitted2, obj.Permitted3);
                    break;
                case 1205:
                    message = string.Format("The field '{0}' with value '{1}' is not patchable.", obj.Field, obj.Value);
                    break;
                case 1206:
                    message = string.Format("The field '{0}' with value '{1}' must contain at least one letter or number.", obj.Field, obj.Value);
                    break;
                case 1207:
                    message = string.Format("The field '{0}' with value '{1}' doesn't meet the requirements of {2} digits between the range {3} and {4}.", obj.Field, obj.Value, obj.Count, obj.Min, obj.Max);
                    break;
                case 1208:
                    message = string.Format("The field '{0}' with value '{1}' must be an {2}.", obj.Field, obj.Value, obj.Type);
                    break;
                case 1209:
                    message = string.Format("The field '{0}' is not correct therefore it cannot be patched.", obj.Field);
                    break;
                case 1210:
                    message = string.Format("The field '{0}' can only contain {1}.", obj.Field, obj.Values);
                    break;
                case 1211:
                    message = string.Format("The field '{0}' must be a valid datetime format.", obj.Field, obj.Value, obj.Example);
                    break;
                case 1212:
                    message = string.Format("The field '{0}' with value '{1}' can only contain letters, numbers and underscores.", obj.Field, obj.Value);
                    break;
                case 1213:
                    message = string.Format("An organization name must contain at least one non-special character.");
                    break;
                case 1214:
                    message = string.Format("A project name must contain at least one non-special character.");
                    break;

                // 13XX CODES
                case 1300:
                    message = string.Format("The field '{0}' with id '{1}' doesn't exists.", obj.Field, obj.Value);
                    break;
                case 1302:
                    message = string.Format("The assessor with username '{0}' was not found.", obj.Value);
                    break;
                case 1303:
                    message = string.Format("The action '{0}' is invalid.", obj.Action);
                    break;
                case 1304:
                    message = string.Format("The field {0} is not found.", obj.Field);
                    break;
                case 1305:
                    message = string.Format("The resource '{0}' with value '{1}' was not found inside the parent '{2} with value '{3}'.", obj.Field, obj.Value, obj.Parent, obj.ParentId);
                    break;
                case 1306:
                    message = string.Format("The resource '{0}' with value '{1}' can only be removed if it has no children.", obj.Field, obj.Value);
                    break;
                case 1307:
                    message = string.Format("The specified user(s) cannot be removed, since it would render the organization without members.");
                    break;
                case 1308:
                    message = string.Format("An organization must have at least one member.");
                    break;
                case 1309:
                    message = string.Format("The specified user(s) cannot be removed, since it would render the project without managers.");
                    break;
                case 1310:
                    message = string.Format("A project must have at least one member.");
                    break;
                case 1311:
                    message = string.Format("The user '{0}' is already in this organization.", obj.UserName);
                    break;
                case 1312:
                    message = string.Format("The user '{0}' is already in this project.", obj.UserName);
                    break;

                // 14XX CODES
                case 1401:
                    message = string.Format("The user '{0}' does not exist.", obj.UserName);
                    break;
                case 1402:
                    message = string.Format("The user '{0}' is not in this organization.", obj.UserName);
                    break;
                case 1403:
                    message = string.Format("The user '{0}' is not in this project.", obj.UserName);
                    break;

                // 15XX CODES
                case 1500:
                    message = string.Format("Exception: {0}", obj.Exception);
                    break;
                case 1501:
                    message = string.Format("Message: {0}", obj.Message);
                    break;

            }
            return message;
        }
    }
}
