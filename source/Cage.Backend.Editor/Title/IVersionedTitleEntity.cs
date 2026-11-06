namespace Cage.Backend.Editor.Title;

public interface IVersionedTitleEntity
{
  string PartitionKey { get; set; }
  string RowKey { get; set; }
  string Name { get; set; }
  bool IsDeleted { get; set; }
}
