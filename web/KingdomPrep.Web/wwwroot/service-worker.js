const CACHE_VERSION = 'nyansapo-erp-pwa-v3';
const APP_SHELL = [
    '/offline.html',
    '/manifest.json',
    '/app.css',
    '/images/icon_bg.png',
    '/icons/icon-192.png',
    '/icons/icon-512.png'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_VERSION)
            .then(cache => Promise.all(
                APP_SHELL.map(url =>
                    cache.add(new Request(url, { cache: 'reload' }))
                        .catch(error => console.warn('PWA shell cache skipped:', url, error))
                )
            ))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys
                    .filter(key => key !== CACHE_VERSION)
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') {
        return;
    }

    const requestUrl = new URL(event.request.url);

    if (requestUrl.origin !== self.location.origin) {
        event.respondWith(fetch(event.request));
        return;
    }

    if (requestUrl.pathname.startsWith('/_blazor')) {
        return;
    }

    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request)
                .catch(() => caches.match('/offline.html'))
        );
        return;
    }

    event.respondWith(
        caches.match(event.request).then(cached => {
            const networkFetch = fetch(event.request)
                .then(response => {
                    if (response.ok) {
                        const copy = response.clone();
                        caches.open(CACHE_VERSION).then(cache => cache.put(event.request, copy));
                    }

                    return response;
                })
                .catch(() => cached);

            return cached || networkFetch;
        })
    );
});
