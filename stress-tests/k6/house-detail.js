import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5183';

export const options = {
  scenarios: {
    detail: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 20 },
        { duration: '30s', target: 50 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.01'],
  },
};

export function setup() {
  // Fetch a page of houses to get real IDs
  const res = http.get(`${BASE_URL}/api/houses?page=1&pageSize=50`);
  const body = JSON.parse(res.body);
  const items = body.items || body.Items || body || [];
  return { houseIds: Array.isArray(items) ? items.map((h) => h.id || h.Id) : [] };
}

export default function (data) {
  if (!data.houseIds || data.houseIds.length === 0) {
    sleep(1);
    return;
  }

  const id = data.houseIds[Math.floor(Math.random() * data.houseIds.length)];

  // House detail
  const detailRes = http.get(`${BASE_URL}/api/houses/${id}`);
  check(detailRes, {
    'detail status 200': (r) => r.status === 200,
    'detail < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(0.3);

  // Availability check
  const availRes = http.get(`${BASE_URL}/api/houses/${id}/availability`);
  check(availRes, {
    'availability status 200': (r) => r.status === 200,
  });

  sleep(0.5);
}
