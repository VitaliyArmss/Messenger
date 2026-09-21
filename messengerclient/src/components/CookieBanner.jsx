import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';

const COOKIE_CONSENT_KEY = 'cookie-consent-accepted';

const CookieBanner = () => {
    const location = useLocation();
    const [isVisible, setIsVisible] = useState(false);

    const isAuthPage =
        location.pathname === '/login' ||
        location.pathname === '/register';

    useEffect(() => {
        const consentAccepted = localStorage.getItem(COOKIE_CONSENT_KEY);

        setIsVisible(isAuthPage && consentAccepted !== 'true');
    }, [isAuthPage]);

    const handleAccept = () => {
        localStorage.setItem(COOKIE_CONSENT_KEY, 'true');
        setIsVisible(false);
    };

    if (!isVisible) {
        return null;
    }

    return (
        <aside className="cookie-banner" role="dialog" aria-label="Уведомление об использовании файлов cookie">
            <div className="cookie-banner-content">
                <p>
                    Мы используем файлы cookie и похожие технологии для работы сайта.
                    Подробнее — в{' '}
                    <Link to="/privacy">
                        политике конфиденциальности
                    </Link>.
                </p>

                <button type="button" onClick={handleAccept}>
                    Окей
                </button>
            </div>
        </aside>
    );
};

export default CookieBanner;