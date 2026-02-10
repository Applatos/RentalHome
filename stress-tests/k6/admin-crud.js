import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5183';
const ADMIN_USER = __ENV.ADMIN_USER || 'admin';
const ADMIN_PASS = __ENV.ADMIN_PASS || 'Sommerhus123!';

export const options = {
  scenarios: {
    admin: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 10 },
        { duration: '30s', target: 20 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.05'],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/admin/auth/login`,
    JSON.stringify({ username: ADMIN_USER, password: ADMIN_PASS }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (loginRes.status !== 200) {
    console.error(`Login failed: ${loginRes.status} ${loginRes.body}`);
    return { token: '' };
  }

  const body = JSON.parse(loginRes.body);
  return { token: body.token || body.Token || '' };
}

export default function (data) {
  if (!data.token) {
    sleep(1);
    return;
  }

  const headers = {
    Authorization: `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  // List houses (admin)
  const listRes = http.get(`${BASE_URL}/api/admin/houses?page=1&pageSize=20`, { headers });
  check(listRes, {
    'admin list ok': (r) => r.status === 200,
    'admin list < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(0.5);

  // List features
  const featRes = http.get(`${BASE_URL}/api/admin/features`, { headers });
  check(featRes, {
    'admin features ok': (r) => r.status === 200,
  });

  sleep(0.3);

  // List areas
  const areaRes = http.get(`${BASE_URL}/api/admin/areas`, { headers });
  check(areaRes, {
    'admin areas ok': (r) => r.status === 200,
  });

  sleep(0.5);
}
