namespace Cage.Backend.Editor.Title;

public interface IVersionedTitleEntityDao<TEntity> where TEntity : IVersionedTitleEntity
{
  Task Add(TEntity entity);
  Task<List<TEntity>> QueryByTitleVersion(string titleVersionId);
  Task<TEntity?> Get(string titleVersionId, string entityId);
  Task Update(string titleVersionId, TEntity entity);
  Task<bool> Delete(string titleVersionId, string entityId);
  Task DeleteByTitleVersion(string titleVersionId);
}
