<?php
/**
 * Republie des pages perle-rare.com dont le HTML vient d'être mis à jour.
 * Réservé au CLI. Appelé par l'API après écriture de la fiche conseiller.
 *
 *   PR_PURGE_URLS  URL https://perle-rare.com/fr|en/slug/
 *   PR_PURGE_IDS   identifiants de pages WordPress (cartes secteurs, etc.)
 */

if (PHP_SAPI !== 'cli') {
    fwrite(STDERR, "cli-only\n");
    exit(1);
}

$root = '/var/www/perle-rare.com-v2/web';
$_SERVER['HTTP_HOST'] = 'perle-rare.com';
$_SERVER['HTTPS'] = 'on';
$_SERVER['REQUEST_URI'] = '/';
require $root . '/wp-load.php';

$urls = array();

$raw_urls = getenv('PR_PURGE_URLS');
if (is_string($raw_urls)) {
    foreach (preg_split('/\s+/', trim($raw_urls)) as $url) {
        if (pr_purge_url_ok($url)) {
            $urls[] = $url;
        }
    }
}

$raw_ids = getenv('PR_PURGE_IDS');
if (is_string($raw_ids) && function_exists('get_permalink')) {
    foreach (preg_split('/\s+/', trim($raw_ids)) as $id) {
        if ($id === '' || !ctype_digit($id)) {
            continue;
        }
        $link = get_permalink((int) $id);
        if (is_string($link) && pr_purge_url_ok($link)) {
            $urls[] = $link;
        }
    }
}

$urls = array_values(array_unique($urls));
if (!$urls) {
    echo "none\n";
    exit(0);
}
if (!function_exists('nitropack_sdk_purge')) {
    fwrite(STDERR, "no-nitro\n");
    exit(3);
}
foreach ($urls as $url) {
    nitropack_sdk_purge($url);
    echo "purged\n";
}

function pr_purge_url_ok($url)
{
    $parts = wp_parse_url((string) $url);
    if (!is_array($parts) || empty($parts['scheme']) || empty($parts['host'])) {
        return false;
    }
    if (strtolower((string) $parts['scheme']) !== 'https') {
        return false;
    }
    if (strtolower((string) $parts['host']) !== 'perle-rare.com') {
        return false;
    }
    if (!empty($parts['user']) || !empty($parts['pass']) || !empty($parts['query'])) {
        return false;
    }
    $path = isset($parts['path']) ? (string) $parts['path'] : '/';
    if (strpos($path, '..') !== false) {
        return false;
    }
    if (preg_match('#^/(wp-admin|wp-login|wp-content|wp-includes)(/|$)#i', $path)) {
        return false;
    }
    return true;
}
