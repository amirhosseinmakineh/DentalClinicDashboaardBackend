# WebOne inbound SMS leads

Webhook endpoint:

```http
GET /api/sms/webone/receive?key=YOUR_SECRET&text=5&from=09123456789&to=YOUR_LINE
```

Configure on the backend host:

- `WebOneSms__WebhookSecret` (required): a strong random value kept out of source control.
- `WebOneSms__DedicatedLine` (optional): the exact recipient number from WebOne's `to` parameter. If set, other recipient numbers are ignored.

The WebOne panel must preserve the configured URL's existing `key` query parameter when appending `text`, `from`, and `to`. Verify this with a live webhook test before enabling. Without the key, requests are rejected (401); without server configuration, the endpoint returns 503.

Messages equal to 5 (ASCII, Persian or Arabic-Indic digits) after trimming create a real-time lead with source `SmsPanel = 5`, name `پنل پیامکی(5)`, and normalized Iranian mobile number. Duplicate phone numbers are not inserted. Invalid text/phone/recipient return HTTP 200 without insertion. Database errors return HTTP 503.

The existing `consultation` rate-limiting policy is applied. Ensure HTTPS and redact the `key` query value from reverse-proxy and application access logs. The `to` field is not an authentication mechanism.

Before rollout: verify that the existing lead dispatch process accepts source 5, that the frontend displays it correctly, and that any SQL CHECK constraint on `LeadAssignments.SourceType` permits 5. A real inbound SMS test and successful builds are still required.
