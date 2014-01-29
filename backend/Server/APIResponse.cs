using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fdocheck.Server
{
    class APIResponse
    {
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
        public object Result { get; set; }
        public string ResultType { get { return Result != null ? Result.GetType().Name : null; } }

        public APIResponse(object result, bool isError = false, string errorMsg = null)
        {
            Error = isError;
            ErrorMessage = errorMsg;
            Result = result;
        }
    }
}
