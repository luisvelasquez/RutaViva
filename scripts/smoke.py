#!/usr/bin/env python3
"""Prueba HTTP contra la solución iniciada. Solo usa la biblioteca estándar."""
import concurrent.futures, datetime, json, sys, urllib.request, urllib.error, uuid
base = sys.argv[1].rstrip('/') if len(sys.argv) > 1 else 'http://localhost:5000'
def get():
    with urllib.request.urlopen(base + '/api/dashboard') as response: return json.load(response)
def post(path, body, key=None, expected=200):
    req = urllib.request.Request(base + '/api' + path, json.dumps(body).encode(),
        {'Content-Type': 'application/json', 'Idempotency-Key': key or str(uuid.uuid4())})
    try:
        with urllib.request.urlopen(req) as response:
            assert response.status == expected
            return json.load(response)
    except urllib.error.HTTPError as error:
        assert error.code == expected, (error.code, error.read().decode())
        return json.load(error)
suffix = uuid.uuid4().hex[:8]
v = post('/vehicles', {'plate': 'T' + suffix, 'workshop': 'Taller prueba', 'odometer': 0})
id = v['id']; today = get()['today']
r = post(f'/vehicles/{id}/routines', {'name': 'Aceite', 'intervalKm': 100, 'intervalDays': 180, 'lastServiceKm': 0, 'lastServiceDate': today})
key = str(uuid.uuid4())
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
    responses = list(pool.map(lambda _: post(f'/vehicles/{id}/readings', {'kilometres': 90}, key), range(8)))
assert all(x == responses[0] for x in responses)
assert len([e for e in get()['events'] if e['aggregateId'] == r['id'] and e['type'] == 'MaintenanceDueSoon']) == 1
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
    list(pool.map(lambda km: post(f'/vehicles/{id}/readings', {'kilometres': km}), [100, 120, 105, 111, 100, 119, 118, 101]))
d = get(); assert next(x for x in d['vehicles'] if x['id'] == id)['odometer'] == 120
orders = [x for x in d['orders'] if x['routineId'] == r['id']]; assert len(orders) == 1
o = orders[0]
def action(name, **values):
    global o
    o = post(f"/orders/{o['id']}/{name}", {'expectedVersion': o['version'], **values})
action('quote', amount=1500000); assert o['state'] == 'Approved'
post(f"/orders/{o['id']}/start", {'expectedVersion': 1}, expected=409)
action('start'); post(f"/orders/{o['id']}/cancel", {'expectedVersion': o['version']}, expected=422)
key = str(uuid.uuid4()); body = {'expectedVersion': o['version'], 'kilometres': 120, 'date': today}
first = post(f"/orders/{o['id']}/complete", body, key)
assert first == post(f"/orders/{o['id']}/complete", body, key)
assert first['state'] == 'Completed'
post(f"/orders/{o['id']}/complete", {**body, 'kilometres': 121}, key, expected=409)
post(f'/vehicles/{id}/readings', {'kilometres': 210})
assert len([e for e in get()['events'] if e['aggregateId'] == r['id'] and e['type'] == 'MaintenanceDueSoon']) == 2
print('OK: API, umbrales, concurrencia, idempotencia, versiones, finalización y nuevo ciclo.')
