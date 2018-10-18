namespace BackMeUp.Messages
{
    public class PostBackData<T>
    {
        public PostBackData(string actionType, T data)
            => (ActionType, Data) = (actionType, data);

        public string ActionType { get; set; }

        public T Data { get; set; }
    }
}