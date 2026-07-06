import http from 'k6/http';
import { check, group, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5075';
const profile = (__ENV.K6_PROFILE || 'full').toLowerCase();

const fullStages = [
  { duration: '5m', target: 100 },
  { duration: '10m', target: 500 },
  { duration: '2m', target: 50 },
];

const ciStages = [
  { duration: '1m', target: 30 },
  { duration: '2m', target: 30 },
  { duration: '30s', target: 0 },
];

export const options = {
  stages: profile === 'ci' ? ciStages : fullStages,
  thresholds: {
    http_req_failed: ['rate<0.02'],
    'http_req_duration{endpoint:read}': ['p(95)<500'],
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
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'auth' } },
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

  group('read paths', () => {
    check(http.get(`${baseUrl}/health/ready`, { tags: { endpoint: 'read' } }), {
      health_ready: (r) => r.status === 200,
    });

    check(http.get(`${baseUrl}/api/v1/EnhancementRequests?page=1&pageSize=25`, { headers, tags: { endpoint: 'read' } }), {
      requests_list: (r) => r.status === 200 || r.status === 401,
    });

    check(http.get(`${baseUrl}/api/v1/applications`, { headers, tags: { endpoint: 'read' } }), {
      applications_list: (r) => r.status === 200 || r.status === 401,
    });
  });

  sleep(0.5);
}
