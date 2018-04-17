/**
 * This module binds cleanup handlers to the node.js process.
 */
'use strict';

const POSIX = {
	SIGHUP: 1,
	SIGINT: 2,
	SIGKILL: 9,
	SIGUSR2: 31,
	SIGTERM: 15
};

module.exports = (cleanup) => {
    if (typeof cleanup !== 'function') {
        return;
    }

    process.on('beforeExit', () => {
        const _cleanup = cleanup();

        if (!(_cleanup instanceof Promise)) {
            return;
        }

        _cleanup.then(()=>{}, ()=>{});
    });
        
    for (const [signal, code] of Object.entries(POSIX)) {
        process.on(signal, () => {
            const _cleanup = cleanup();

            if (!(_cleanup instanceof Promise)) {
                return;
            }

            _cleanup.then(() => process.exit(128 + code), ()=>{});
        });
    }
};
