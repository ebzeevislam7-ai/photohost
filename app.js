// === Supabase config ===
const SUPABASE_URL = 'https://yinsvtpftqprixmecjyl.supabase.co';
const SUPABASE_ANON_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlpbnN2dHBmdHFwcml4bWVjanlsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzEwOTc5MzcsImV4cCI6MjA4NjY3MzkzN30.3mYO5Kceea-K7u-4QW4UlskCnxyx4KAnFfuzcrUXimc';

// Initialize Supabase - wait for library to load from unpkg
let supabaseClient = null;

function initSupabase() {
    if (!supabaseClient && typeof window.supabase !== 'undefined') {
        try {
            supabaseClient = window.supabase.createClient(SUPABASE_URL, SUPABASE_ANON_KEY);
            console.log('✓ Supabase initialized successfully');
            initApp();
            return true;
        } catch (e) {
            console.error('✗ Supabase init error:', e);
            return false;
        }
    }
    return supabaseClient !== null;
}

// Wait for library to load
let loadAttempts = 0;
const supabaseCheck = setInterval(() => {
    loadAttempts++;
    console.log(`Checking for Supabase (attempt ${loadAttempts})...`);
    
    if (typeof window.supabase !== 'undefined') {
        console.log('✓ Supabase library loaded from unpkg');
        clearInterval(supabaseCheck);
        initSupabase();
    } else if (loadAttempts > 200) {
        clearInterval(supabaseCheck);
        console.error('✗ Supabase library failed to load after 10 seconds');
    }
}, 50);

// === App State ===
let currentUser = null;

// === Initialization ===
async function initApp() {
    if (!supabaseClient) {
        console.error('✗ Supabase not initialized');
        return;
    }
    
    console.log('Initializing app...');
    const { data: { user } } = await supabaseClient.auth.getUser();
    if (user) {
        console.log('✓ User found:', user.email);
        currentUser = user;
        showApp();
    } else {
        console.log('No user session found, showing auth');
        showAuth();
    }

    // react to auth changes
    supabaseClient.auth.onAuthStateChange((_event, session) => {
        console.log('Auth state changed:', _event, session?.user?.email);
        if (session?.user) {
            currentUser = session.user;
            showApp();
        } else {
            currentUser = null;
            showAuth();
        }
    });
}

document.addEventListener('DOMContentLoaded', function() {
    if (supabaseClient) {
        initApp();
    }
});

// === UI Toggle ===
function toggleForms() {
    document.getElementById('loginForm').style.display = 
        document.getElementById('loginForm').style.display === 'none' ? 'block' : 'none';
    document.getElementById('registerForm').style.display = 
        document.getElementById('registerForm').style.display === 'none' ? 'block' : 'none';
}

function showAuth() {
    console.log('Showing auth screen');
    // keep the auth container as flex so centering and responsive sizing work
    document.getElementById('authContainer').style.display = 'flex';
    document.getElementById('appContainer').style.display = 'none';
    document.getElementById('navbar').style.display = 'none';
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
}

function showApp() {
    console.log('Showing app screen for user:', currentUser?.email);
    document.getElementById('authContainer').style.display = 'none';
    document.getElementById('appContainer').style.display = 'block';
    document.getElementById('navbar').style.display = 'block';
    
    const userName = currentUser.firstName || currentUser.email;
    document.getElementById('navUserName').textContent = userName;
    
    loadPhotos();
    loadProfileData();
}

// === Alert Messages ===
function showAlert(message, type = 'success') {
    console.log(`[${type.toUpperCase()}]`, message);
    const alertHTML = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    const container = document.getElementById('alertContainer');
    if (container) {
        container.innerHTML = alertHTML;
    } else {
        console.warn('Alert container not found');
    }
    
    setTimeout(() => {
        const alert = document.querySelector('.alert');
        if (alert) alert.remove();
    }, 5000);
}

// === Registration & Login ===
async function handleRegister(event) {
    event.preventDefault();
    console.log('✓ handleRegister triggered');
    
    if (!supabaseClient) {
        console.warn('⚠ Supabase client not ready');
        showAlert('Система загружается, подождите...', 'warning');
        return;
    }

    const firstName = document.getElementById('regFirstName').value.trim();
    const lastName = document.getElementById('regLastName').value.trim();
    const email = document.getElementById('regEmail').value.trim().toLowerCase();
    const password = document.getElementById('regPassword').value;
    const passwordConfirm = document.getElementById('regPasswordConfirm').value;

    console.log('Registering:', { firstName, lastName, email });

    if (!firstName || !lastName || !email || !password) {
        showAlert('Заполните все поля', 'danger');
        return;
    }
    if (password !== passwordConfirm) { showAlert('Пароли не совпадают', 'danger'); return; }
    if (password.length < 6) { showAlert('Пароль должен быть минимум 6 символов', 'danger'); return; }

    try {
        const { data, error } = await supabaseClient.auth.signUp({
            email,
            password,
            options: { data: { firstName, lastName, bio: '' } }
        });

        if (error) {
            console.error('Registration error:', error);
            showAlert(error.message, 'danger');
            return;
        }

        console.log('✓ Registration successful');

        // Clear form
        document.getElementById('regFirstName').value = '';
        document.getElementById('regLastName').value = '';
        document.getElementById('regEmail').value = '';
        document.getElementById('regPassword').value = '';
        document.getElementById('regPasswordConfirm').value = '';

        // Supabase sends confirmation email depending on your project settings.
        showAlert('Регистрация выполнена. Подтвердите Email, если требуется.', 'success');
    } catch (e) {
        console.error('✗ Registration exception:', e);
        showAlert('Ошибка регистрации: ' + e.message, 'danger');
    }
}

async function handleLogin(event) {
    event.preventDefault();
    console.log('✓ handleLogin triggered');
    
    if (!supabaseClient) {
        console.warn('⚠ Supabase client not ready');
        showAlert('Система загружается, подождите...', 'warning');
        return;
    }
    
    const email = document.getElementById('loginEmail').value.trim().toLowerCase();
    const password = document.getElementById('loginPassword').value;

    console.log('Logging in:', { email });

    try {
        const { data, error } = await supabaseClient.auth.signInWithPassword({ email, password });
        if (error) { 
            console.error('Login error:', error);
            showAlert(error.message, 'danger'); 
            return; 
        }

        console.log('✓ Login successful');
        currentUser = data.user;
        showAlert(`Добро пожаловать, ${currentUser.user_metadata?.firstName || currentUser.email}!`, 'success');
        showApp();
    } catch (e) {
        console.error('✗ Login exception:', e);
        showAlert('Ошибка входа: ' + e.message, 'danger');
    }
}

async function logout() {
    if (supabaseClient) {
        await supabaseClient.auth.signOut();
    }
    currentUser = null;
    showAuth();
    showAlert('Вы вышли из аккаунта', 'info');
}

// === Photos Management ===
async function handleUpload(event) {
    event.preventDefault();
    if (!supabaseClient) { showAlert('Система загружается, подождите...', 'warning'); return; }
    if (!currentUser) { showAlert('Сначала войдите в систему', 'warning'); return; }
    const files = document.getElementById('fileInput').files;
    if (files.length === 0) { showAlert('Выберите файлы для загрузки', 'warning'); return; }

    const uploaded = [];
    for (const file of Array.from(files)) {
        const ext = file.name.split('.').pop();
        const fileName = `${Date.now()}-${Math.random().toString(36).slice(2)}.${ext}`;
        const path = `${currentUser.id}/${fileName}`;

        const { error } = await supabaseClient.storage.from('photos').upload(path, file, { cacheControl: '3600', upsert: false });
        if (error) {
            console.error('Upload error', error);
            showAlert(`Ошибка загрузки ${file.name}: ${error.message}`, 'danger');
            continue;
        }

        const { data: urlData } = supabaseClient.storage.from('photos').getPublicUrl(path);
        uploaded.push({ fileName: file.name, url: urlData.publicUrl, path });
    }

    if (uploaded.length > 0) {
        showAlert(`Успешно загружено ${uploaded.length} фото!`, 'success');
        document.getElementById('fileInput').value = '';
        document.getElementById('preview').innerHTML = '';
        await loadPhotos();
    }
}

function handleFileChange() {
    const files = document.getElementById('fileInput').files;
    const preview = document.getElementById('preview');
    preview.innerHTML = '';

    Array.from(files).slice(0, 6).forEach(file => {
        const reader = new FileReader();
        reader.onload = function(e) {
            const img = document.createElement('img');
            img.src = e.target.result;
            preview.appendChild(img);
        };
        reader.readAsDataURL(file);
    });
}

async function loadPhotos() {
    if (!currentUser) return;
    const grid = document.getElementById('photoGrid');

    // list files in user's folder
    const { data, error } = await supabaseClient.storage.from('photos').list(currentUser.id, { limit: 100, sortBy: { column: 'name', order: 'desc' } });
    if (error) {
        console.error('List error', error);
        grid.innerHTML = '<div class="col-12"><div class="alert alert-light text-center">Не удалось загрузить список фото. Убедитесь, что бакет "photos" создан и публичен.</div></div>';
        return;
    }

    const photos = data || [];
    document.getElementById('photoCount').textContent = photos.length;

    if (photos.length === 0) {
        grid.innerHTML = '<div class="col-12"><div class="alert alert-light text-center">Нет загруженных фотографий. Начните с загрузки первой!</div></div>';
        return;
    }

    grid.innerHTML = '';
    for (const file of photos) {
        const path = `${currentUser.id}/${file.name}`;
        const { data: urlData } = supabaseClient.storage.from('photos').getPublicUrl(path);
        const publicUrl = urlData.publicUrl;

        const col = document.createElement('div');
        col.className = 'col';
        col.innerHTML = `
            <div class="card h-100 shadow-sm">
                <img src="${publicUrl}" class="card-img-top" alt="${file.name}" style="cursor:pointer;">
                <div class="card-body">
                    <h6 class="card-title text-truncate" title="${file.name}">${file.name}</h6>
                    <p class="text-muted mb-2 small">${new Date(file.created_at).toLocaleString('ru-RU')}</p>
                    <button class="btn btn-sm btn-outline-danger w-100">Удалить</button>
                </div>
            </div>`;

        // wire delete
        col.querySelector('button').addEventListener('click', async () => {
            if (!confirm('Удалить это фото?')) return;
            const { error } = await supabaseClient.storage.from('photos').remove([path]);
            if (error) { showAlert('Ошибка при удалении: ' + error.message, 'danger'); return; }
            showAlert('Фото удалено', 'success');
            await loadPhotos();
        });

        grid.appendChild(col);
    }
}

function deletePhoto(photoId) {
    if (!confirm('Вы уверены, что хотите удалить это фото?')) return;

    const userPhotos = getUserPhotos();
    const filtered = userPhotos.filter(p => p.id !== photoId);
    localStorage.setItem(`photos_${currentUser.id}`, JSON.stringify(filtered));
    loadPhotos();
    showAlert('Фото удалено', 'success');
}

// === Profile Management ===
async function loadProfileData() {
    if (!currentUser) return;
    // fetch fresh user metadata
    const { data: { user }, error } = await supabaseClient.auth.getUser();
    if (error) { console.error(error); return; }
    const meta = user.user_metadata || {};
    document.getElementById('profileEmail').value = user.email || '';
    document.getElementById('profileFirstName').value = meta.firstName || '';
    document.getElementById('profileLastName').value = meta.lastName || '';
    document.getElementById('profileBio').value = meta.bio || '';
}

async function saveProfile() {
    if (!currentUser) return;
    const firstName = document.getElementById('profileFirstName').value.trim();
    const lastName = document.getElementById('profileLastName').value.trim();
    const bio = document.getElementById('profileBio').value.trim();

    const { data, error } = await supabaseClient.auth.updateUser({ data: { firstName, lastName, bio } });
    if (error) { showAlert('Ошибка сохранения профиля: ' + error.message, 'danger'); return; }

    currentUser = data.user;
    document.getElementById('navUserName').textContent = firstName || currentUser.email;
    const modal = bootstrap.Modal.getInstance(document.getElementById('profileModal'));
    if (modal) modal.hide();
    showAlert('Профиль обновлён!', 'success');
}

// === Image Modal ===
function showImage(src, fileName) {
    let modal = document.getElementById('imageModal');
    if (!modal) {
        const div = document.createElement('div');
        div.id = 'imageModal';
        div.className = 'modal fade';
        div.tabIndex = '-1';
        div.innerHTML = `
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">${fileName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img src="${src}" style="max-width:100%; max-height:600px;" alt="${fileName}">
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(div);
        modal = div;
    }

    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

// File input preview
document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
        fileInput.addEventListener('change', handleFileChange);
    }
});
