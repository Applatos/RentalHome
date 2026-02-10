import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5183';

export const options = {
  scenarios: {
    browse: {
      executor: 'constant-vus',
      vus: 100,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.02'],
  },
};

export default function () {
  // Simulate browsing: search → pick result → view detail → check availability
  const page = Math.floor(Math.random() * 10) + 1;
  const searchRes = http.get(`${BASE_URL}/api/houses?page=${page}&pageSize=20`);
  check(searchRes, {
    'search ok': (r) => r.status === 200,
  });

  sleep(0.5 + Math.random());

  // Try to parse a house ID from results
  try {
    const body = JSON.parse(searchRes.body);
    const items = body.items || body.Items || body || [];
    if (Array.isArray(items) && items.length > 0) {
      const house = items[Math.floor(Math.random() * items.length)];
      const id = house.id || house.Id;

      const detailRes = http.get(`${BASE_URL}/api/houses/${id}`);
      check(detailRes, {
        'detail ok': (r) => r.status === 200,
      });

      sleep(0.3 + Math.random() * 0.5);

      const availRes = http.get(`${BASE_URL}/api/houses/${id}/availability`);
      check(availRes, {
        'avail ok': (r) => r.status === 200,
      });
    }
  } catch (_) {
    // ignore parse errors
  }

  sleep(0.5 + Math.random());
}
