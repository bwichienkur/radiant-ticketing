import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5075';

export const options = {
  stages: [
    { duration: '5m', target: 100 },
    { duration: '10m', target: 500 },
    { duration: '2m', target: 50 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<500'],
  },
};

function login() {
  const res = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({
      email: __ENV.TEST_USER || 'admin@local.test',
      password: __ENV.TEST_PASSWORD || 'Admin123!',
    }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (res.status !== 200) {
    return null;
  }

  return res.json('token');
}

export function setup() {
  return { token: login() };
}

export default function (data) {
  const headers = data.token ? { Authorization: `Bearer ${data.token}` } : {};

  check(http.get(`${baseUrl}/health`), { health: (r) => r.status === 200 });
  check(http.get(`${baseUrl}/api/v1/applications`, { headers }), {
    applications: (r) => r.status === 200 || r.status === 401,
  });
  check(http.get(`${baseUrl}/api/v1/EnhancementRequests`, { headers }), {
    requests: (r) => r.status === 200 || r.status === 401,
  });

  sleep(0.5);
}
