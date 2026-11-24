<?php
/**
 * Plugin Name: Perle Rare — phrase Céline du pied de page
 * Description: Retire du pied de page la phrase « Prenez contact avec Céline » et sa version anglaise « Get in touch with Céline ».
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Retire la phrase de contact avec Céline. Le paragraphe de recrutement reste.
 * Un second passage ne change plus rien.
 */
function perle_rare_footer_celine_hide($html)
{
    if (!is_string($html) || $html === '') {
        return $html;
    }
    $pattern = '~<p\b[^>]*>\s*(?:Prenez contact avec|Get in touch with)\s+C(?:é|e|&eacute;|&#233;)line\b.*?</p>~isu';
    $replaced = preg_replace($pattern, '', $html);
    return is_string($replaced) ? $replaced : $html;
}

foreach (array('widget_text', 'widget_text_content', 'widget_custom_html_content', 'widget_block_content') as $perle_rare_footer_celine_hook) {
    add_filter($perle_rare_footer_celine_hook, 'perle_rare_footer_celine_hide', 20);
}
unset($perle_rare_footer_celine_hook);
