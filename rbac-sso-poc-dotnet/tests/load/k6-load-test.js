/**
 * k6 Load Test Script for RBAC-SSO Multi-Tenant E-Commerce POC
 * k6 負載測試腳本
 *
 * Prerequisites:
 * - k6 installed (https://k6.io/docs/getting-started/installation/)
 * - Services running (docker-compose up)
 * - Test users configured in Keycloak
 *
 * Usage:
 * k6 run tests/load/k6-load-test.js
 * k6 run --vus 100 --duration 5m tests/load/k6-load-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const loginDuration = new Trend('login_duration');
const createProductDuration = new Trend('create_product_duration');
const listProductsDuration = new Trend('list_products_duration');
const getProductDuration = new Trend('get_product_duration');

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || 'http://localhost:8180';

// Test users with different roles
const USERS = [
    { username: 'admin', password: 'admin', role: 'ADMIN' },
    { username: 'tenant-a-admin', password: 'password', role: 'TENANT_ADMIN' },
    { username: 'tenant-b-admin', password: 'password', role: 'TENANT_ADMIN' },
    { username: 'user-a', password: 'password', role: 'USER' },
    { username: 'viewer', password: 'password', role: 'VIEWER' },
];

// Test options
export const options = {
    stages: [
        { duration: '30s', target: 10 },   // Ramp up to 10 users
        { duration: '1m', target: 50 },    // Ramp up to 50 users
        { duration: '2m', target: 100 },   // Ramp up to 100 users
        { duration: '2m', target: 100 },   // Stay at 100 users
        { duration: '30s', target: 0 },    // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],  // 95% of requests should be under 500ms
        http_req_failed: ['rate<0.01'],    // Error rate should be under 1%
        errors: ['rate<0.01'],
        login_duration: ['p(95)<1000'],    // Login should be under 1s
        create_product_duration: ['p(95)<500'],
        list_products_duration: ['p(95)<300'],
        get_product_duration: ['p(95)<200'],
    },
};

// Setup function - runs once before test
export function setup() {
    console.log('Load test starting...');
    console.log(`Base URL: ${BASE_URL}`);
    console.log(`Keycloak URL: ${KEYCLOAK_URL}`);

    // Verify services are up
    const healthCheck = http.get(`${BASE_URL}/health`);
    if (healthCheck.status !== 200) {
        throw new Error('Gateway health check failed');
    }

    return { startTime: new Date().toISOString() };
}

// Main test function - runs for each VU
export default function() {
    // Select random user
    const user = USERS[Math.floor(Math.random() * USERS.length)];

    group('Authentication Flow', function() {
        // Login
        const loginStart = Date.now();
        const loginRes = login(user.username, user.password);
        loginDuration.add(Date.now() - loginStart);

        if (!loginRes.success) {
            errorRate.add(1);
            return;
        }

        const token = loginRes.token;
        errorRate.add(0);

        // Perform operations based on role
        group('Product Operations', function() {
            performProductOperations(token, user.role);
        });

        sleep(Math.random() * 2 + 1); // Random sleep 1-3 seconds
    });
}

// Login function
function login(username, password) {
    const res = http.post(
        `${BASE_URL}/api/auth/login`,
        JSON.stringify({ username, password }),
        {
            headers: { 'Content-Type': 'application/json' },
        }
    );

    const success = check(res, {
        'login successful': (r) => r.status === 200,
        'token received': (r) => r.json('data.accessToken') !== undefined,
    });

    if (success) {
        return {
            success: true,
            token: res.json('data.accessToken'),
        };
    }

    console.error(`Login failed for ${username}: ${res.status}`);
    return { success: false };
}

// Product operations based on role
function performProductOperations(token, role) {
    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
    };

    // All roles can list products
    group('List Products', function() {
        const start = Date.now();
        const res = http.get(`${BASE_URL}/api/products?size=20`, { headers });
        listProductsDuration.add(Date.now() - start);

        check(res, {
            'list products successful': (r) => r.status === 200,
            'products returned': (r) => r.json('data.items') !== undefined,
        });
    });

    // ADMIN, TENANT_ADMIN, USER can create products
    if (['ADMIN', 'TENANT_ADMIN', 'USER'].includes(role)) {
        group('Create Product', function() {
            const product = {
                name: `Load Test Product ${Date.now()}`,
                price: Math.random() * 1000 + 10,
                category: ['Electronics', 'Clothing', 'Food'][Math.floor(Math.random() * 3)],
                description: 'Product created during load test',
            };

            const start = Date.now();
            const res = http.post(
                `${BASE_URL}/api/products`,
                JSON.stringify(product),
                { headers }
            );
            createProductDuration.add(Date.now() - start);

            const created = check(res, {
                'create product successful': (r) => r.status === 201,
                'product id returned': (r) => r.json('data.productId') !== undefined,
            });

            if (created) {
                const productId = res.json('data.productId');

                // Get the created product
                group('Get Product', function() {
                    const getStart = Date.now();
                    const getRes = http.get(`${BASE_URL}/api/products/${productId}`, { headers });
                    getProductDuration.add(Date.now() - getStart);

                    check(getRes, {
                        'get product successful': (r) => r.status === 200,
                        'product data correct': (r) => r.json('data.name') === product.name,
                    });
                });

                // ADMIN, TENANT_ADMIN can update products
                if (['ADMIN', 'TENANT_ADMIN', 'USER'].includes(role)) {
                    group('Update Product', function() {
                        const update = {
                            name: `Updated ${product.name}`,
                            price: product.price * 1.1,
                            category: product.category,
                        };

                        const updateRes = http.put(
                            `${BASE_URL}/api/products/${productId}`,
                            JSON.stringify(update),
                            { headers }
                        );

                        check(updateRes, {
                            'update product successful': (r) => r.status === 200,
                        });
                    });
                }

                // Only ADMIN, TENANT_ADMIN can delete
                if (['ADMIN', 'TENANT_ADMIN'].includes(role)) {
                    group('Delete Product', function() {
                        const deleteRes = http.del(`${BASE_URL}/api/products/${productId}`, null, { headers });

                        check(deleteRes, {
                            'delete product successful': (r) => r.status === 204,
                        });
                    });
                }
            }
        });
    }

    // VIEWER can only read
    if (role === 'VIEWER') {
        group('Viewer Read-Only Operations', function() {
            // Try to create (should fail)
            const createRes = http.post(
                `${BASE_URL}/api/products`,
                JSON.stringify({ name: 'Forbidden', price: 10, category: 'Test' }),
                { headers }
            );

            check(createRes, {
                'viewer cannot create (403)': (r) => r.status === 403,
            });
        });
    }
}

// Teardown function - runs once after test
export function teardown(data) {
    console.log(`Load test completed. Started at: ${data.startTime}`);
    console.log(`Ended at: ${new Date().toISOString()}`);
}

// Summary function - customizes output
export function handleSummary(data) {
    const summary = {
        metrics: {
            http_req_duration_p95: data.metrics.http_req_duration.values['p(95)'],
            http_req_failed: data.metrics.http_req_failed.values.rate,
            error_rate: data.metrics.errors.values.rate,
            login_duration_p95: data.metrics.login_duration.values['p(95)'],
            create_product_duration_p95: data.metrics.create_product_duration.values['p(95)'],
            list_products_duration_p95: data.metrics.list_products_duration.values['p(95)'],
            get_product_duration_p95: data.metrics.get_product_duration.values['p(95)'],
        },
        thresholds_passed: Object.values(data.root_group.checks).every(c => c.passes === c.fails + c.passes),
    };

    return {
        'stdout': JSON.stringify(summary, null, 2),
        'tests/load/results/summary.json': JSON.stringify(data, null, 2),
    };
}
