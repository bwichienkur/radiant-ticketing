import http from 'k6/http';
import { check, group, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5076';
const profile = (__ENV.K6_PROFILE || 'gate').toLowerCase();

const gateStages = [
  { duration: '2m', target: 100 },
  { duration: '5m', target: 500 },
  { duration: '1m', target: 0 },
];

const prStages = [
  { duration: '45s', target: 50 },
  { duration: '90s', target: 100 },
  { duration: '30s', target: 0 },
];

export const options = {
  stages: profile === 'pr' ? prStages : gateStages,
  thresholds: {
    http_req_failed: ['rate<0.02'],
    'http_req_duration{endpoint:list}': ['p(95)<2000'],
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

  group('postgres read paths', () => {
    check(http.get(`${baseUrl}/health/ready`, { tags: { endpoint: 'read' } }), {
      health_ready: (r) => r.status === 200,
    });

    check(
      http.get(`${baseUrl}/api/v1/EnhancementRequests?page=1&pageSize=25`, {
        headers,
        tags: { endpoint: 'list' },
      }),
      {
        requests_list: (r) => r.status === 200 || r.status === 401,
      },
    );

    check(http.get(`${baseUrl}/api/v1/applications`, { headers, tags: { endpoint: 'list' } }), {
      applications_list: (r) => r.status === 200 || r.status === 401,
    });
  });

  sleep(0.3);
}
