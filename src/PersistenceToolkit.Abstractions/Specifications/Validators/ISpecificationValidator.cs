namespace PersistenceToolkit.Abstractions.Specifications;

public interface ISpecificationValidator
{
    bool IsValid<T>(T entity, ISpecification<T> specification);
}
