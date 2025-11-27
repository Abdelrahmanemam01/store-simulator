module PriceCalculator

open Cart


let calculateCheckoutTotal (cart: Cart) (discountPercentage: decimal) : decimal =
   
    let subTotal = getTotalPrice cart
    
   
    let finalTotal = applyPrecentageDiscount discountPercentage cart
    

    finalTotal
