# Secretary reservation follow-up API

## Update a follow-up

`PUT /api/Reservation/SecretaryAnnouncement` requires an authenticated user. The server records that user's id and UTC update time. `reservationId` is the reservation's existing numeric identifier.

```json
{
  "reservationId": 42,
  "status": "Confirmed",
  "description": "بیمار تایید کرد پنجشنبه ساعت 10 مراجعه می‌کند"
}
```

Valid status values are `NotCalled`, `NoAnswer`, `Confirmed`, `CancelledByPatient`, `RescheduleRequested`, and `CallAgain`. Whitespace is trimmed, null or whitespace clears the description, and descriptions are limited to 1000 characters. Deleted or cancelled reservations are rejected.

## Push notification contract

The push envelope contains `title`, `body`, and `data`. Frontends should route on `data.type`. The following `data` contracts are stable:

| Secretary status | `type` | Additional data |
| --- | --- | --- |
| `NoAnswer` | `ReservationSecretaryNoAnswer` | `reservationId`, `patientName`, `reservationDate` (`yyyy-MM-dd`), `message` |
| `Confirmed` | `ReservationSecretaryConfirmed` | `reservationId`, `patientName`, `message` |
| `CancelledByPatient` | `ReservationSecretaryCancelled` | `reservationId`, `patientName`, `message` |

## Lists, filtering, and summary

`GET /api/reservations` accepts `search`, `fromDate`, `toDate`, `consultantId`, `secretaryAnnouncementStatus`, `reservationStatus`, `pageNumber`, and `pageSize`. Search matches patient name or phone number. `reservationStatus` accepts `Active` or `Cancelled`.

Reservation list items include `secretaryAnnouncementStatus`, `secretaryAnnouncement`, `secretaryAnnouncementUpdatedAt`, and `secretaryAnnouncementUserName`.

`GET /api/secretary/dashboard/summary` returns `needCall`, `confirmed`, `noAnswer`, and `cancelled` counts for non-deleted, active reservations.
