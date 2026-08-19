namespace POS.Domain.Entities
{
    public enum CashMovementType
    {
        // Cash added to the drawer outside of a sale — e.g. topping up
        // change float, a bank deposit reversed back into the drawer.
        CashIn,

        // Cash removed from the drawer outside of a sale — e.g. a paid-out
        // expense, a bank drop.
        CashOut,
    }
}
