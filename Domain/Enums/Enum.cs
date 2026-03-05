using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum SpecialtyType { Style, Technique, Occasion, Segment }
    public enum RfqSelectionMethod { ManualSearch, SystemSuggest, Repurchase }
    public enum UserRole { Customer, ShopOwner, Staff, Admin }
    public enum ShopStatus { Pending, Active, Suspended, Rejected }
    public enum AssetType { Base, Texture, Topping, Decoration, Text }
    public enum RfqType { Direct, Public }
    public enum RfqStatus { Open, Closed, Cancelled }
    public enum QuoteStatus { Pending, Accepted, Rejected, Expired }
    public enum OrderStatus { PendingPayment, Paid, InProduction, Shipped, Delivered, Completed, Cancelled, Dispute }
    public enum PaymentMethod { VnPay, MoMo, Wallet }
    public enum ProductVisibility { Private, Unlisted, Public }
    public enum ProductStatus { Draft, Active, Inactive }
}
