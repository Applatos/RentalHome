import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5183';

export const options = {
  scenarios: {
    search: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 20 },
        { duration: '30s', target: 50 },
        { duration: '20s', target: 100 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<200'],
    http_req_failed: ['rate<0.01'],
  },
};

const searchTerms = ['pool', 'sauna', 'beach', 'family', 'modern', 'romantic'];

export default function () {
  // Public house search
  const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
  const res = http.get(`${BASE_URL}/api/houses?search=${term}&page=1&pageSize=20`);
  check(res, {
    'search status 200': (r) => r.status === 200,
    'search < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(0.5);

  // Public features (searchable)
  const featRes = http.get(`${BASE_URL}/api/features/searchable`);
  check(featRes, {
    'features status 200': (r) => r.status === 200,
  });

  sleep(0.3);
}
