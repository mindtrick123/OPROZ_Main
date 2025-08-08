# OPROZ Razorpay Payment Integration

This documentation describes the Razorpay payment integration implemented in the OPROZ SaaS platform.

## Features Implemented

### 1. Payment Processing
- Complete Razorpay integration with order creation and payment verification
- Support for multiple subscription plans (Monthly, Quarterly, Yearly)
- Discount/offer system integration
- Secure payment signature verification
- Webhook support for payment notifications

### 2. User Interface
- Subscription plan selection page
- Payment checkout integration with Razorpay
- Payment success confirmation page
- Payment history dashboard
- Mobile-responsive design

### 3. Backend Services
- `IRazorpayService` interface and `RazorpayService` implementation
- `PaymentController` for handling payment workflows
- Payment history tracking and reporting
- Secure webhook handling

## File Structure

### Controllers
- `Controllers/PaymentController.cs` - Main payment processing controller

### Services
- `Services/IRazorpayService.cs` - Payment service interface
- `Services/RazorpayService.cs` - Razorpay API implementation

### Models
- `Models/PaymentHistory.cs` - Payment tracking model (already existed)
- `ViewModels/PaymentViewModel.cs` - Payment view models

### Views
- `Views/Payment/Subscribe.cshtml` - Subscription selection page
- `Views/Payment/PaymentSuccess.cshtml` - Payment confirmation page
- `Views/Payment/History.cshtml` - Payment history dashboard

## Configuration

### Required Settings (appsettings.json)
```json
{
  "RazorpaySettings": {
    "KeyId": "rzp_test_xxxxxxxxxxxxxxxx",
    "KeySecret": "your-razorpay-test-secret",
    "WebhookSecret": "your-webhook-secret",
    "EnableTestMode": true
  }
}
```

### Dependencies Added
- `Razorpay` NuGet package (version 3.0.0)

## Key Endpoints

- `GET /Payment/Subscribe/{planId}` - Show subscription page
- `POST /Payment/InitiatePayment` - Create Razorpay order
- `POST /Payment/VerifyPayment` - Verify payment signature
- `GET /Payment/PaymentSuccess/{id}` - Show payment confirmation
- `GET /Payment/History` - Show payment history
- `POST /Payment/Webhook` - Handle Razorpay webhooks

## Security Features

- Payment signature verification using HMAC-SHA256
- Anti-forgery tokens for all payment forms
- User authentication required for all payment operations
- Secure webhook handling

## Testing

To test the payment integration:

1. Update Razorpay credentials in `appsettings.json`
2. Register a new user account
3. Navigate to Services page
4. Select a subscription plan
5. Complete payment using Razorpay test cards
6. Verify payment history and success pages

## Test Cards (Razorpay)

- **Success**: 4111 1111 1111 1111
- **Failure**: 4111 1111 1111 1112
- **CVV**: Any 3 digits
- **Expiry**: Any future date

## Production Deployment

1. Replace test credentials with live Razorpay credentials
2. Set `EnableTestMode` to `false` in configuration
3. Configure webhook URLs in Razorpay dashboard
4. Test all payment flows in staging environment

## Notes

- The Razorpay SDK has some .NET Framework compatibility warnings but works correctly with .NET 8
- Console logging is used instead of ILogger extensions due to SDK compatibility
- Subscription cancellation is implemented as a placeholder (returns true)
- Invoice download functionality is prepared but not fully implemented

## Future Enhancements

- Invoice PDF generation
- Email notifications for payments
- Subscription management (upgrade/downgrade)
- Refund processing UI
- Payment analytics dashboard
- Multi-currency support