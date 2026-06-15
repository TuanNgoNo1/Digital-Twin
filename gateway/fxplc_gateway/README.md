# Parallel fxplc gateway

This gateway preserves the existing HSL gateway and provides an MIT-licensed
alternative using `KrystianD/fxplc`.

## Safety model

- Both gateways use port `5000`, but only the selected service runs.
- The systemd services conflict, so only one may own the SC09 serial port.
- Set `FXPLC_ALLOW_WRITES=0` for read-only validation or `1` after write tests pass.
- Caddy remains pointed at HSL until fxplc read/write tests pass.

## Switch and rollback

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc
curl -s http://127.0.0.1:5000/debug

/home/admin/PiGatewayFxplc/switch-gateway.sh hsl
curl -s http://127.0.0.1:5000/debug

/home/admin/PiGatewayFxplc/switch-gateway.sh restart
/home/admin/PiGatewayFxplc/switch-gateway.sh reset-sc09
/home/admin/PiGatewayFxplc/switch-gateway.sh status
```

Do not enable writes until read-only values match the HSL baseline.
