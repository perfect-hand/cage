namespace Cage.Backend.Editor.Title;

public interface IVersionedTitleEntity
{
  string PartitionKey { get; set; }
  string RowKey { get; set; }
  string Name { get; set; }
  bool IsDeleted { get; set; }
}

public interface IVersionedTitleEntityDao<TEntity> where TEntity : IVersionedTitleEntity
{
  Task Add(TEntity entity);
  Task<List<TEntity>> QueryByTitleVersion(string titleVersionId);
  Task<TEntity?> Get(string titleVersionId, string entityId);
  Task Set(string titleVersionId, TEntity entity);
  Task<bool> Delete(string titleVersionId, string entityId);
  Task DeleteByTitleVersion(string titleVersionId);
}
