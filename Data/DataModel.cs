using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public abstract class DataModel
{
    internal abstract void Create(ModelBuilder modelBuilder);
}

public abstract class DataModel<T> : DataModel
    where T : class
{
    internal override void Create(ModelBuilder modelBuilder) =>
        Create(modelBuilder.Entity<T>());

    protected virtual void Create(EntityTypeBuilder<T> model)
    {
        return;
    }
}

public static class ModelBuilderExtension
{
    public static void Create(this ModelBuilder builder, DataModel model) =>
        model.Create(builder);
}