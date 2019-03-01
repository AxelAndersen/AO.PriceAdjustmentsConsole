namespace AO.PriceAdjustments.Services
{
    public interface IPriceService
    {
        void EnsureAllEntitiesExist();
        void GetData();
        void GetNewPricedItems();
        void SaveCompetitorPrices();
    }
}