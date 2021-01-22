
namespace WallTec.CoreCom.Models
{
    public class Result<T>
    {
        public Result()
        {
            Model = default(T);
            WasSuccessful = false;
        }
        public Result(T model)
        {
            Model = model;
            WasSuccessful = true;
        }
       
        public Result(bool wasSuccessful)
        {

            WasSuccessful = wasSuccessful;
        }
        public Result(string userMessage="",string developerMessage="", int errorCode = 0)
        {
            ErrorCode = errorCode;
            DeveloperMessage = developerMessage;
            UserMessage = userMessage;
            WasSuccessful = false;
        }
        public T Model { get; set; }

       

        public bool WasSuccessful { get; set; }

        public string DeveloperMessage { get; set; }
        public string UserMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
