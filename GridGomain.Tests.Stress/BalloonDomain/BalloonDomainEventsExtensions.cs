using GridDomain.Tests.Unit.BalloonDomain.Events;

namespace GridGomain.Tests.Stress.BalloonDomain
{
    public static class BalloonDomainEventsExtensions
    {
        public static BalloonCatalogItem ToCatalogItem(this BalloonCreated e)
        {
            return new BalloonCatalogItem()
                   {
                       BalloonId = e.SourceId,
                       LastChanged = e.CreatedTime,
                       Title = e.Value
                   };
        }

        public static BalloonCatalogItem ToCatalogItem(this BalloonTitleChanged e)
        {
            return new BalloonCatalogItem()
                   {
                       BalloonId = e.SourceId,
                       LastChanged = e.CreatedTime,
                       Title = e.Value
                   };
        }
    }
}