# WebOne incoming SMS webhook

Configure the following URL in the WebOne panel's **لینک وب هوک** field:

```text
https://YOUR_BACKEND_DOMAIN/api/sms/webone/receive
```

WebOne sends an HTTP GET request with exactly the documented query parameters:

```http
GET /api/sms/webone/receive?text=5&from=09123456789&to=10002
```

No `key` or `WebhookSecret` is required. The `text` parameter is the SMS body, `from` is the sender phone, and `to` is the recipient line.

Optional backend environment variable: `WebOneSms__DedicatedLine=10002`. If set, messages received on other lines are ignored; if not set, all recipient lines are eligible.

Only a message equal to 5 (ASCII, Persian, or Arabic-Indic digits, with surrounding whitespace ignored) from a valid Iranian mobile phone creates a lead. Leads use `SmsPanel = 5`, `RealTime`, `RequiresThreeMinuteCall = true`, and `New`. Existing phone numbers are not inserted again. Other messages and invalid phones return HTTP 200 without insertion; database failures return HTTP 503.

The existing consultation rate limiter is applied. The WebOne documentation does not specify authentication or request signing; therefore this endpoint cannot authenticate WebOne as the sender. Use HTTPS and consider provider IP allowlisting only if WebOne supplies authoritative IP ranges. Checking the `to` value does not authenticate a request.

Before deployment, verify the lead-dispatch path and frontend handling of source 5, any SQL CHECK constraint, and successful build and live WebOne delivery.
