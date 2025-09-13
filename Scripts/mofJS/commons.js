/**
* Author: Jeraldy Matara Deus | deusjeraldy@gmail.com
* Contains Global Reusable Functions
*/

function toLabel(number) {
    number = number || 0
    return number.toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    })
}

function toNumber(number) {
    number = number || 0
    return parseFloat(number.toString().split(",").join(""))
}

function withholdingTemplate(d) {
    //if (d['hasWithHolding'] && d["LiquidatedDemageAmount"] != null) {
    //    return '<br>\
    //            <i class="wh-holding">\
    //                <table>\
    //                   <tr>\
    //                     <td><div class="bubble-w"></div></td>\
    //                     <td>Withholding</td>\
    //                   </tr>\
    //                     <tr>\
    //                     <td><div class="bubble-w"></div></td>\
    //                     <td>Liquidated Demage</td>\
    //                   </tr>\
    //               </table>\
    //            </i>';
    //} else if (d['hasWithHolding']) {
    //    return '<br>\
    //            <i class="wh-holding">\
    //                <table>\
    //                   <tr>\
    //                     <td><div class="bubble-w"></div></td>\
    //                     <td>Withholding</td>\
    //                   </tr>\
    //               </table>\
    //            </i>';
    //} else if (d["LiquidatedDemageAmount"] != null) {
    //    return '<br>\
    //            <i class="wh-holding">\
    //                <table>\
    //                     <tr>\
    //                     <td><div class="bubble-w"></div></td>\
    //                     <td>Liquidated Demage</td>\
    //                   </tr>\
    //               </table>\
    //            </i>';
    //} else {
    //    return '';
    //}
    return ''
}


function calculate(d) {
    var A = toLabel(d["OperationalWithHoldingAmount"] || 0)
    var B = toLabel(d["LiquidatedDemageAmount"] || 0);
    var C = toLabel(d["RetentionAmount"] || 0)
    var C1 = toLabel(d["MiscDeduction"] || 0)
    var C2 = toLabel(d["AdvancePayment"] || 0)

    var D = toNumber(A) + toNumber(B) + toNumber(C) + toNumber(C1) + toNumber(C2)
    var E = toLabel(d['OperationalAmount'])
    var Z = toNumber(E) - D

    if (A == 0 && B == 0 && C == 0 && C1 == 0 && C2 == 0) {
        return '-'
    }

    return '<details>\
           <summary>Open.</summary>\
        <table>\
                          <tr>\
                             <td class="td-calc">WithHolding</td>\
                             <td class="td-calc"></td>\
                             <td class="td-calc">' + A + '</td>\
                          </tr>\
                          <tr>\
                            <td class="td-calc">Liquidated</td>\
                            <td class="td-calc"></td>\
                            <td class="td-calc">'+ toLabel(B) + '</td>\
                          </tr>\
                          <tr>\
                            <td class="td-calc">Rentation</td>\
                            <td class="td-calc"></td>\
                            <td class="td-calc">'+ toLabel(C) + '</td>\
                          </tr>\
                          <tr>\
                            <td class="td-calc">MiscDeduction</td>\
                            <td class="td-calc"></td>\
                            <td class="td-calc">'+ toLabel(C1) + '</td>\
                          </tr>\
                          <tr>\
                            <td class="td-calc">Adv.Paymt</td>\
                            <td class="td-calc"></td>\
                            <td class="td-calc">'+ toLabel(C2) + '</td>\
                          </tr>\
                          <tr style="background-color:#ccc">\
                             <td class="td-calc">Total</td>\
                             <td class="td-calc">(D)</td>\
                             <td class="td-calc">'+ toLabel(D) + '</td>\
                           </tr>\
                           <tr>\
                             <td class="td-calc">Voucher</td>\
                             <td class="td-calc">(E)</td>\
                             <td class="td-calc">' + E + '</td>\
                           </tr>\
                           <tr style="background-color:#ccc">\
                             <td class="td-calc">Payable</td>\
                             <td class="td-calc">(E-D)</td> \
                             <td class="td-calc">' + toLabel(Z) + '</td>\
                           </tr>\
                       </table>\
            </details>';
}