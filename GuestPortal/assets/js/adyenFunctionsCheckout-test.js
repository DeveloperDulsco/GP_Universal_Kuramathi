
const httpPost = (endpoint, data) => fetch(`${endpoint}`, {
    method: "POST",
    body: JSON.stringify(data),
    headers: {
        Accept: 'application/json, text/plain, */*',
        'Content-Type': 'application/json'
    }
}).then(response => response.json());

const getOrginkey = (domainName) => httpPost(BaseURL + '/api/portalservice/orginkey?domainName=' + domainName).then(response => {
    var keys = JSON.parse(response);
    return keys.originKeys[Object.keys(keys.originKeys)[0]];
});

const getPaymentMethods = () => httpPost(BaseURL + '/api/portalservice/getPaymentmethods').then(response => {
    return JSON.parse(response);
});

const getCostEstimater = (costestimatorPostData) => httpPost(BaseURL + '/api/portalservice/getCostEstimater', costestimatorPostData).then(response => {
    return response;
});

const makePayment = (cardData, ) => httpPost(BaseURL + '/Payment/MakePaymentCheckout', cardData).then(response => {
    return response;
});



const InsertPaymentHeaderDetails = (paymentData, ConfirmationNo, transactionID, ReservationNameID, TransactionType) => httpPost(BaseURL + '/Checkout/InsertPaymentResponseHeader?ConfirmationNo=' + ConfirmationNo + '&TransactionID=' + transactionID + '&ReservationNameID=' + ReservationNameID + '&TransactionType=' + TransactionType, paymentData).then(response => {
    return response;
});

const SaveAdyenPaymentDetails = (paymentData, ConfirmationNo, transactionID, ReservationNameID, TransactionType) => httpPost(BaseURL + '/Checkout/SaveAdyenPaymentDetails?ConfirmationNo=' + ConfirmationNo + '&TransactionID=' + transactionID + '&ReservationNameID=' + ReservationNameID + '&TransactionType=' + TransactionType, paymentData).then(response => {
    return response;
});




var currentDropIn;
var makePaymmentResponse;
var makePaymentRequest;
var dropin;
var transactionID = moment().format('MMDDhhmmss');
var transactionType = "Sale";

getOrginkey(BaseURL).then(orginKey => {
    getPaymentMethods().then(paymentMethodsResponse => {

        const configuration = {
            paymentMethodsResponse: paymentMethodsResponse, // The `/paymentMethods` response from the server.
            originKey: orginKey,
            locale: "en-US",
            environment: "test",
            onSubmit: (state, dropin) => {
                console.log('onsubmit')
                $('.loader-screen').fadeIn(100);

                transactionType = "Sale";
                transactionID = moment().format('MMDDhhmmss');

                makePaymentRequest = {

                    makePaymentRequest: state.data,
                    ConfirmationNo: ReservationNumber,
                    AmountToCharge: Amount,
                    MerchantReference: ReservationNumber + '-' + ReservationNameID,
                    FundingSource: "DEBIT", //Set to debit during checkout will make sale transaction always
                    TransctionID: transactionID,
                    ReservationNameID: ReservationNameID
                }

                makePayment(makePaymentRequest).then(response => {

                    if (response.result) {
                        if (response.isRedirect) {
                            var responseJson = JSON.parse(response.adyenResponse);
                            SaveAdyenPaymentDetails(responseJson, ReservationNumber, transactionID, ReservationNameID, transactionType).then(saveresponse => {
                                dropin.handleAction(responseJson.action);
                            });
                        }
                        else {
                            InsertPaymentHeaderDetails(response.adyenResponse.ResponseObject, ReservationNumber, transactionID, ReservationNameID, transactionType).then(saveresponse => {
                                $('.loader-screen').fadeOut(500);
                                moveToNextTab($('#paymentNextButton'));
                                $('#paymentStatusMessage').html("Your payment was successful.");
                                $('#paymentStatusModal').modal('show');
                            });
                        }
                    }
                    else {
                        $('#customMessageModalMessage').html(response.message)
                        $('#customMessageModal').modal('show');

                        $('.loader-screen').fadeOut(100);
                    }

                }).catch(error => {
                    $('.loader-screen').fadeOut(500);
                    throw Error(error);
                });

            },
            onAdditionalDetails: (state, dropin) => {

                // Your function calling your server to make a `/payments/details` request
                makeDetailsCall(state.data).then(response => {
                    if (response.action) {
                        // Drop-in handles the action object from the /payments response
                        dropin.handleAction(response.action);
                    } else {
                        // Your function to show the final result to the shopper
                        showFinalResult(response);
                    }
                }).catch(error => {
                    throw Error(error);
                });
            },
            paymentMethodsConfiguration: {
                card: { // Example optional configuration for Cards
                    hasHolderName: false,
                    holderNameRequired: true,
                    enableStoreDetails: false,
                    hideCVC: false, // Change this to true to hide the CVC field for stored cards
                    name: 'Credit or debit card'
                }
            }
        };

        const checkout = new AdyenCheckout(configuration);
        dropin = checkout.create('dropin').mount('#dropin-container');

       
    });
});



function getActiveTransctions() {

}


function proceedWithPayment() {
    $('.loader-screen').fadeIn(100);
    
}


function updateReservation() {
    httpPost(BaseURL + '/Home/UpdateReservation').then(response => {
        return response;
    }).error(error => {
        console.log(error);
        console.error(error);
    });
}