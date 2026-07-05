import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5075';
const vus = Number(__ENV.VUS || 10);
const duration = __ENV.DURATION || '30s';

export const options = {
  vus,
  duration,
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
  },
};

function login() {
  const res = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({
      email: __ENV.TEST_USER || 'admin@enhancementhub.dev',
      password: __ENV.TEST_PASSWORD || 'password123',
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

  const health = http.get(`${baseUrl}/health/ready`);
  check(health, { 'health ok': (r) => r.status === 200 });

  const requests = http.get(`${baseUrl}/api/v1/EnhancementRequests`, { headers });
  check(requests, {
    'v1 requests reachable': (r) => r.status === 200,
  });

  const applications = http.get(`${baseUrl}/api/v1/applications`, { headers });
  check(applications, {
    'v1 applications reachable': (r) => r.status === 200,
  });

  sleep(1);
}
