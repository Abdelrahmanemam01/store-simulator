module PriceCalculator

open Cart

//-------------------------------------
// 1) Fixed-percentage discount function
//-------------------------------------
let calculateCheckoutTotalWithPercentage (cart: Cart) (discountPercentage: decimal) : decimal =
    let subTotal = getTotalPrice cart
    let finalTotal = applyPercentageDiscount discountPercentage cart
    finalTotal


//-------------------------------------
// 2) Tiered discount helper
//-------------------------------------
let private getDiscountPercentage (subTotal: decimal) : decimal =
    if subTotal >= 100_000M then 20M
    elif subTotal >= 50_000M then 10M
    elif subTotal >= 20_000M then 5M
    else 0M


//-------------------------------------
// 3) Tiered discount total
//-------------------------------------
let calculateCheckoutTotal (cart: Cart) : decimal =
    let subTotal = getTotalPrice cart
    let discountPercentage = getDiscountPercentage subTotal
    let discountAmount = subTotal * (discountPercentage / 100M)
    let finalTotal = subTotal - discountAmount
    finalTotal


//-------------------------------------
// 4) Optional: return detailed info
//-------------------------------------
let calculateCheckoutTotalWithDetails (cart: Cart) : decimal * decimal * decimal * decimal =
    let subTotal = getTotalPrice cart
    let discountPercentage = getDiscountPercentage subTotal
    let discountAmount = subTotal * (discountPercentage / 100M)
    let finalTotal = subTotal - discountAmount
    (subTotal, discountPercentage, discountAmount, finalTotal)
