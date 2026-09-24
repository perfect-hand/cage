namespace Cage.Backend.Editor.Organization;

public class OrganizationMapper
{
    public OrganizationDto ToDto(OrganizationEntity entity)
    {
        return new OrganizationDto
        {
            Id = entity.RowKey,
            Name = entity.Name
        };
    }

    public OrganizationEntity ToEntity(OrganizationDto dto)
    {
        return new OrganizationEntity
        {
            PartitionKey = "Organization",
            RowKey = dto.Id ?? Guid.NewGuid().ToString(),
            Name = dto.Name
        };
    }
}
