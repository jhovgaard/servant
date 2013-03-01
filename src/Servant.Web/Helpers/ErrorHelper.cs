using System.Collections.Generic;
using System.Linq;
using Servant.Business.Objects;
using Nancy.Validation;

namespace Servant.Web.Helpers
{
    public static class ErrorHelper
    {
        public static IEnumerable<Error> ConvertValidationResultToErrorList(ModelValidationResult result)
        {
            if (result == null)
                return null;

            return
                from error in result.Errors
                from memberName in error.MemberNames
                select new Error(false, error.GetMessage(memberName), memberName);
        }
    }
}