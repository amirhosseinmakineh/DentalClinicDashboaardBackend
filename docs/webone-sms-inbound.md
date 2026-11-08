# WebOne SMS inbound leads

Configure the following **server-side environment variables** (do not commit real values):

- `WebOneSms__WebhookSecret`: a long, randomly generated secret.
- `WebOneSms__DedicatedLine`: the exact dedicated SMS line number (the incoming `to` value).

Configure the WebOne inbound URL (if the provider preserves existing query parameters):

```text
https://api.drsaeedmoghadam.com/api/sms/webone/receive?key=YOUR_SECRET
```

WebOne appends `text`, `from`, and `to`. Confirm this behavior with a real delivery before enabling a campaign. If the provider removes the `key` parameter, the endpoint intentionally returns HTTP 401 and a different authentication mechanism must be agreed upon.

Accepted messages: `5`, `۵`, `٥` (after trimming). Valid Iranian mobile numbers are normalized. Leads are stored as `UserName = پنل پیامکی(5)`, `SourceType = SmsPanel (5)`, with existing real-time assignment and phone de-duplication.

Unexpected content, invalid phone, or a different recipient returns HTTP 200 without creating a lead. An unconfigured endpoint returns 503; an invalid secret returns 401; database failures return 503.

**Security:** HTTPS is required. Query secrets may appear in reverse-proxy/access logs; redact the `key` query parameter in logging and restrict access to logs. Consider IP allowlisting only after WebOne supplies verified source IPs. The `to` parameter is not authentication.

No schema migration is required if `LeadAssignments.SourceType` is an integer column with no CHECK constraint restricting values to 1–4. Verify before production rollout.
