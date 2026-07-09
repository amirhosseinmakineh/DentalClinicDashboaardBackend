/* eslint-disable no-restricted-globals */

const REALTIME_LEAD_TAG_PREFIX = 'realtime-lead-';

function parsePushPayload(event) {
  if (!event.data) {
    return { title: '', body: '', data: {} };
  }

  try {
    const payload = event.data.json();
    return {
      title: payload.title ?? '',
      body: payload.body ?? '',
      data: payload.data ?? {}
    };
  } catch {
    const text = event.data.text();
    return { title: 'اعلان', body: text, data: {} };
  }
}

function notifyClients(message) {
  return self.clients
    .matchAll({ type: 'window', includeUncontrolled: true })
    .then((clients) => {
      clients.forEach((client) => client.postMessage(message));
    });
}

function closeRealtimeLeadNotifications(leadId) {
  const tag = `${REALTIME_LEAD_TAG_PREFIX}${leadId}`;
  return self.registration.getNotifications({ tag }).then((notifications) => {
    notifications.forEach((notification) => notification.close());
  });
}

self.addEventListener('push', (event) => {
  const payload = parsePushPayload(event);
  const data = payload.data ?? {};
  const type = data.type ?? '';

  if (type === 'RealtimeLeadTaken') {
    const leadId = data.leadId;
    event.waitUntil(
      closeRealtimeLeadNotifications(leadId).then(() =>
        notifyClients({
          type: 'RealtimeLeadTaken',
          leadId: Number(leadId)
        })
      )
    );
    return;
  }

  if (type === 'RealtimeLead') {
    const leadId = data.leadId;
    const tag = `${REALTIME_LEAD_TAG_PREFIX}${leadId}`;

    event.waitUntil(
      self.registration
        .showNotification(payload.title || 'لید جدید!', {
          body: payload.body || 'یک لید لحظه‌ای آماده دریافت است. سریع برداریدش!',
          tag,
          renotify: true,
          requireInteraction: true,
          silent: false,
          vibrate: [300, 120, 300, 120, 300],
          data,
          actions: [
            { action: 'pickup', title: 'برداریدش!' },
            { action: 'dismiss', title: 'بستن' }
          ]
        })
        .then(() =>
          notifyClients({
            type: 'RealtimeLead',
            leadId: Number(leadId),
            title: payload.title,
            body: payload.body
          })
        )
    );
    return;
  }

  if (type === 'offline_leads') {
    event.waitUntil(
      self.registration
        .showNotification(payload.title || 'لید آفلاین جدید!', {
          body: payload.body || 'لیدهای آفلاین جدید برای شما ثبت شده است.',
          tag: 'offline-leads',
          renotify: true,
          data
        })
        .then(() =>
          notifyClients({
            type: 'OfflineLeads',
            count: Number(data.count ?? 0)
          })
        )
    );
    return;
  }

  if (payload.title || payload.body) {
    event.waitUntil(
      self.registration.showNotification(payload.title || 'اعلان', {
        body: payload.body,
        data
      })
    );
  }
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const data = event.notification.data ?? {};
  const type = data.type ?? '';
  const action = event.action;

  if (type === 'RealtimeLead') {
    const leadId = Number(data.leadId);
    const message =
      action === 'pickup'
        ? { type: 'RealtimeLeadPickup', leadId }
        : { type: 'RealtimeLeadOpen', leadId };

    event.waitUntil(
      notifyClients(message).then(() => {
        if (self.clients.openWindow) {
          return self.clients.openWindow('/dashboard');
        }
        return undefined;
      })
    );
    return;
  }

  if (type === 'offline_leads') {
    event.waitUntil(
      self.clients.openWindow
        ? self.clients.openWindow('/consultant/leadManagment')
        : Promise.resolve()
    );
  }
});

self.addEventListener('message', (event) => {
  const data = event.data ?? {};

  if (data.type === 'CloseRealtimeLeadNotification' && data.leadId) {
    event.waitUntil(closeRealtimeLeadNotifications(data.leadId));
  }
});
