#!/usr/bin/env python3
"""Prueba de navegador opcional. Requiere pip install playwright y playwright install chromium."""
import os
import sys
import uuid
from playwright.sync_api import sync_playwright, expect
base = sys.argv[1].rstrip('/') if len(sys.argv) > 1 else 'http://localhost:8080'
with sync_playwright() as p:
    browser = p.chromium.launch(executable_path=os.environ.get('PLAYWRIGHT_CHROMIUM_EXECUTABLE'), args=['--no-sandbox','--disable-dev-shm-usage'])
    page = browser.new_page(viewport={'width': 1440, 'height': 1000})
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    def post(path, data):
        response = page.request.post(base + '/api' + path, data=data,
            headers={'Idempotency-Key': str(uuid.uuid4())})
        assert response.ok, response.text()
        return response.json()
    today = page.request.get(base + '/api/dashboard').json()['today']
    plate = 'UI' + uuid.uuid4().hex[:7].upper()
    vehicle = post('/vehicles', {'plate': plate, 'workshop': 'Taller UI', 'odometer': 0})
    post('/vehicles/' + vehicle['id'] + '/routines', {'name': 'Prueba de navegador',
        'intervalKm': 100, 'intervalDays': None, 'lastServiceKm': 0, 'lastServiceDate': today})
    page.goto(base)
    page.locator('.vehicle-card').filter(has_text=plate).click()
    page.locator('#reading').fill('100')
    page.get_by_role('button', name='Enviar lectura').click()
    expect(page.get_by_role('status')).to_contain_text('Lectura aceptada')
    page.get_by_role('button', name='Órdenes de servicio').click()
    row = page.locator('tr').filter(has_text=plate)
    row.get_by_role('button', name='Cotizar', exact=True).click()
    dialog = page.get_by_role('dialog')
    dialog.locator('input[name=amount]').fill('1500001')
    dialog.get_by_role('button', name='Enviar cotización').click()
    expect(row).to_contain_text('Por aprobar')
    for button, confirm, state in [('Aprobar', 'Aprobar cotización', 'Aprobada'),
                                  ('Iniciar', 'Iniciar servicio', 'En ejecución'),
                                  ('Completar', 'Completar servicio', 'Completada')]:
        row.get_by_role('button', name=button, exact=True).click()
        page.get_by_role('dialog').get_by_role('button', name=confirm).click()
        expect(row).to_contain_text(state)
    page.get_by_role('button', name='Actividad y eventos').click()
    expect(page.locator('.event-row').first).to_be_visible()
    page.set_viewport_size({'width': 390, 'height': 844})
    page.get_by_role('button', name='Vehículos y rutinas').click()
    assert page.evaluate('document.documentElement.scrollWidth <= innerWidth + 1')
    assert not errors, errors
    browser.close()
print('OK: interfaz, cotización manual, aprobación, ejecución, finalización y vista móvil.')
