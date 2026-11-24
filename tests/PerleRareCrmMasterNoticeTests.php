<?php
/**
 * Avertissement éditeur : le titre sous le nom est une donnée maître du CRM.
 * Usage : php tests/PerleRareCrmMasterNoticeTests.php
 */

define('ABSPATH', __DIR__);

if (!function_exists('add_action')) {
    function add_action($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-linkedin-crm-notice.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n" . var_export($actual, true) . "\n--- expected ---\n" . var_export($expected, true) . "\n");
    }
}

function expect_contains($label, $actual, $needle)
{
    global $failures;
    if (!is_string($actual) || strpos($actual, $needle) === false) {
        $failures++;
        fwrite(STDERR, "FAIL $label\nmissing: $needle\n--- actual ---\n" . var_export($actual, true) . "\n");
    }
}

$profile = perle_rare_crm_master_notice(true);
expect_contains('profile names the title', $profile, 'titre sous le nom');
expect_contains('profile names the CRM', $profile, 'perle-rare.info');
$card = perle_rare_crm_master_notice(false);
expect_contains('card names the CRM', $card, 'perle-rare.info');
expect_same('card does not claim the job title', strpos($card, 'titre sous le nom') === false, true);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-crm-master-notice tests passed\n";
