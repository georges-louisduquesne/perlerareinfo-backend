<?php
/**
 * Plugin Name: Perle Rare — LinkedIn piloté par le CRM
 * Description: Avertit, dans l’éditeur de la page de Georges-Louis Duquesne, que le lien LinkedIn est publié depuis perle-rare.info.
 */

if (!defined('ABSPATH')) {
    exit;
}

add_action('admin_notices', static function () {
    if (!is_admin() || !function_exists('get_current_screen')) {
        return;
    }
    $screen = get_current_screen();
    if (!$screen || $screen->base !== 'post') {
        return;
    }
    $post_id = isset($_GET['post']) ? (int) $_GET['post'] : 0;
    if ($post_id <= 0) {
        return;
    }
    $post = get_post($post_id);
    if (!$post instanceof WP_Post) {
        return;
    }
    if ($post->post_name !== 'georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier') {
        return;
    }
    if (!current_user_can('edit_post', $post_id)) {
        return;
    }
    echo '<div class="notice notice-warning"><p><strong>Lien LinkedIn piloté par perle-rare.info.</strong> Il est enregistré sur la fiche de Georges-Louis Duquesne. Une modification faite ici sera remplacée au prochain enregistrement de cette fiche.</p></div>';
});
