<?php
/**
 * Plugin Name: Perle Rare — fiche Google des cartes virtuelles
 * Description: Empêche NitroPack de figer le HTML des cartes virtuelles. Leur lien Google est lu dans conseillers_personnels à chaque affichage.
 */

if (!defined('ABSPATH')) {
    exit;
}

add_filter('nitropack_can_serve_cache', static function ($can) {
    $uri = isset($_SERVER['REQUEST_URI']) ? (string) $_SERVER['REQUEST_URI'] : '';
    if (preg_match('#/carte-virtuelle-[a-z0-9-]+#i', $uri)) {
        return false;
    }
    return $can;
});
