// Service Worker cho PWA Đạo Dịch Huyền Cơ Pháp - v3.0 (Anti-Cache Realtime)
const CACHE_NAME = 'dao-dich-cache-v3';

self.addEventListener('install', event => {
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cache => {
          if (cache !== CACHE_NAME) {
            console.log('[SW] Đang xóa cache cũ:', cache);
            return caches.delete(cache);
          }
        })
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // TUYỆT ĐỐI KHÔNG CACHE API: Mọi request /api/* luôn đi thẳng ra Network!
  if (url.pathname.startsWith('/api/')) {
    return;
  }

  // Network-first cho các tài nguyên web tĩnh để luôn có giao diện mới nhất
  event.respondWith(
    fetch(event.request)
      .then(response => {
        return response;
      })
      .catch(() => caches.match(event.request))
  );
});
