using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure; // Needed for IConventionContext
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // Good to have if considering converters

namespace Persistance.Conventions;

public class DateTimeToTimestampWithoutTimeZoneConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        // The 'modelBuilder.Metadata' gives you access to the IModel being built.
        // You can iterate through entities and properties from there.
        foreach (IMutableEntityType entityType in modelBuilder.Metadata.GetEntityTypes().Cast<IMutableEntityType>())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                // Check if the property is a DateTime or Nullable<DateTime>
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    // Check if a ColumnType has ALREADY been explicitly set (e.g., by HasColumnType in OnModelCreating)
                    // If not, then apply our default convention.
                    // property.FindAnnotation(...) is the correct way to check existing column type.
                    if (property.FindAnnotation(RelationalAnnotationNames.ColumnType) == null)
                    {
                        // Set the column type directly via annotations.
                        // This is the programmatic equivalent of calling .HasColumnType() in fluent API.
                        property.SetAnnotation(
                            RelationalAnnotationNames.ColumnType,
                            "timestamp without time zone"
                        );
                    }
                }
            }
        }
    }
}