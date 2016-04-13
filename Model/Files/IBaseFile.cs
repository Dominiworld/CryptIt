namespace Model.Files
{
    public interface IBaseFile
    {
        int Id { get; set; }
        string Url { get; set; }
        int OwnerId { get; set; }
        string FileName { get; set; }
    }
}
