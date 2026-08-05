
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

const makePayment = (cardData, ) => httpPost(BaseURL + '/Payment/MakePayment', cardData).then(response => {
    return response;
});

const savePaymentData = (paymentData) => httpPost(BaseURL + '/api/portalservice/savePaymentDetails', paymentData).then(response => {
    return JSON.parse(response);
});

const makeDetailsCall = (paymentData) => httpPost(BaseURL + '/api/portalservice/makePaymentDetails', paymentData).then(response => {
    return JSON.parse(response);
});

const CalculatePreAuthAmount = (paymentData) => httpPost(BaseURL + '/Home/CalculatePreAuthAmount', paymentData).then(response => {
    return response;
});

const SaveAdyenPaymentDetails = (paymentData, ConfirmationNo, transactionID, ReservationNameID, TransactionType) => httpPost(BaseURL + '/Home/SaveAdyenPaymentDetails?ConfirmationNo=' + ConfirmationNo + '&TransactionID=' + transactionID + '&ReservationNameID=' + ReservationNameID + '&TransactionType=' + TransactionType , paymentData).then(response => {
    return response;
});

const InsertPaymentHeaderDetails = (paymentData, ConfirmationNo, transactionID, ReservationNameID, TransactionType) => httpPost(BaseURL + '/Home/InsertPaymentResponseHeader?ConfirmationNo=' + ConfirmationNo + '&TransactionID=' + transactionID + '&ReservationNameID=' + ReservationNameID + '&TransactionType=' + TransactionType, paymentData).then(response => {
    return response;
});

const generateTransactionID = (paymentData) => httpPost(BaseURL + '/Payment/GenerateTransactionID', paymentData).then(response => {
    return JSON.parse(response);
});


function showFinalResult(paymentResult) {

}

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
                $('.loader-screen').fadeIn(100);
                var costestimatorPostData = {
                    Amount : 0,
                    Currency : "SGD",
                    EncryptedCard: state.data.paymentMethod.encryptedCardNumber,
                    Mcc : "8999"
                };

                getCostEstimater(costestimatorPostData).then(estimatorResponse => {

                    var FundingSource = "DEBIT";
                    var PaymentMethod = "";
                    if (estimatorResponse.Result) {

                        FundingSource = estimatorResponse.FundingSource;
                        transactionType = estimatorResponse.TransactionType;
                        PaymentMethod = estimatorResponse.PaymentMethod;
                    }

                    if (PaymentMethod.toUpperCase().includes("JCB") || PaymentMethod.toUpperCase().includes("CUP")) {
                        FundingSource = "DEBIT";
                        transactionType = "Sale";
                    }

                    //Calculate PreAuthAmount
                    var data = {
                        ReservationNumber: ReservationNumber,
                        FundingSource: FundingSource,
                        PaymentMethod: PaymentMethod
                    };
                    
                    CalculatePreAuthAmount(data).then(preauthAmountResponse => {
                        if (preauthAmountResponse.RoomRate > 0) {
                            $('#spanRoomRate').html("SGD " + preauthAmountResponse.RoomRate);
                        }
                        else {
                            $('#spanRoomRate').html("--");
                        }
                        $('#spanIncidential').html("SGD " + preauthAmountResponse.IncidentialCharge + " x " + preauthAmountResponse.NoofNights + " Night(s)");
                        $('#spanTotalAmount').html("SGD " + preauthAmountResponse.PreuthAmount);

                        if (transactionType == "Sale") {
                            $('#idPaymentTitleLable').html('PAYMENT DETAILS');
                            $('#paymentAmountConfirmationModalLabel').html('Please verify your payment details')
                        }
                        else {
                            $('#idPaymentTitleLable').html('PREAUTHORISATION DETAILS');
                            $('#paymentAmountConfirmationModalLabel').html('Please verify your preauthorisation details');
                        }

                        transactionID = moment().format('MMDDhhmmss');

                        makePaymentRequest = {
                            makePaymentRequest: state.data,
                            ConfirmationNo: ReservationNumber,
                            AmountToCharge: preauthAmountResponse.PreuthAmount,
                            MerchantReference: ReservationNumber + '-' + ReservationNameID,
                            FundingSource: FundingSource,
                            TransctionID: transactionID,
                            ReservationNameID : ReservationNameID
                        }

                        //Show Modal dialog
                        $('#paymentAmountConfirmationModal').modal('show');

                        $('.loader-screen').fadeOut(500);

                    }).catch(error => {
                        $('.loader-screen').fadeOut(500);
                        throw Error(error);
                    });

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


function proceedWithPayment() {
    $('.loader-screen').fadeIn(100);
    makePayment(makePaymentRequest).then(response => {

        console.log('Make payment reponse');
        console.log(response)

        if (response.result) {
            if (response.isRedirect) {

                var responseJson = JSON.parse(response.adyenResponse);
                SaveAdyenPaymentDetails(responseJson, ReservationNumber, transactionID, ReservationNameID, transactionType).then(saveresponse => {
                    console.log(responseJson.action);
                    dropin.handleAction(responseJson.action);
                });
            }
            else {
                InsertPaymentHeaderDetails(response.adyenResponse.ResponseObject, ReservationNumber, transactionID, ReservationNameID, transactionType).then(saveresponse => {
                    $('.loader-screen').fadeOut(500);
                    moveToNextTab($('#btnPaymentNextButton'));
                    $('#paymentStatusMessage').html("Your payment was successful.");
                    $('#paymentStatusModal').modal('show');
                    $('#paymentAmountConfirmationModal').modal('hide');
                    showFinalResult(response);
                });
                //need to implement to proceed to next page after save the payment success or failure data
            }
        }
        else {

            $('#customMessageModalMessage').html(response.message)
            $('#customMessageModal').modal('show');
            $('.loader-screen').fadeOut(100);
        }

        // 

    }).catch(error => {
        $('.loader-screen').fadeOut(500);
        throw Error(error);
    });
}


function updateReservation() {
    httpPost(BaseURL + '/Home/UpdateReservation').then(response => {
        return response;
    }).error(error => {
        console.log(error);
        console.error(error);
    });
}