// === Supabase config ===
const SUPABASE_URL = 'https://yinsvtpftqprixmecjyl.supabase.co';
const SUPABASE_ANON_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlpbnN2dHBmdHFwcml4bWVjanlsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzEwOTc5MzcsImV4cCI6MjA4NjY3MzkzN30.3mYO5Kceea-K7u-4QW4UlskCnxyx4KAnFfuzcrUXimc';

// Initialize Supabase - wait for library to load from unpkg
let supabase = null;

function initSupabase() {
    if (!supabase && typeof window.supabase !== 'undefined') {
        try {
            supabase = window.supabase.createClient(SUPABASE_URL, SUPABASE_ANON_KEY);
            console.log('Supabase initialized');
            initApp();
        } catch (e) {
            console.error('Supabase init error:', e);
        }
    }
}

// Wait for library to load
const supabaseCheck = setInterval(() => {
    if (typeof window.supabase !== 'undefined') {
        clearInterval(supabaseCheck);
        initSupabase();
    }
}, 50);

// === App State ===
let currentUser = null;

// === Initialization ===
async function initApp() {
    if (!supabase) {
        console.error('Supabase not initialized');
        return;
    }
    
    const { data: { user } } = await supabase.auth.getUser();
    if (user) {
        currentUser = user;
        showApp();
    } else {
        showAuth();
    }

    // react to auth changes
    supabase.auth.onAuthStateChange((_event, session) => {
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
    if (supabase) {
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
    // keep the auth container as flex so centering and responsive sizing work
    document.getElementById('authContainer').style.display = 'flex';
    document.getElementById('appContainer').style.display = 'none';
    document.getElementById('navbar').style.display = 'none';
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
}

function showApp() {
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
    const alertHTML = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    document.getElementById('alertContainer').innerHTML = alertHTML;
    
    setTimeout(() => {
        const alert = document.querySelector('.alert');
        if (alert) alert.remove();
    }, 5000);
}

// === Registration & Login ===
async function handleRegister(event) {
    event.preventDefault();

    const firstName = document.getElementById('regFirstName').value.trim();
    const lastName = document.getElementById('regLastName').value.trim();
    const email = document.getElementById('regEmail').value.trim().toLowerCase();
    const password = document.getElementById('regPassword').value;
    const passwordConfirm = document.getElementById('regPasswordConfirm').value;

    if (!firstName || !lastName || !email || !password) {
        showAlert('Заполните все поля', 'danger');
        return;
    }
    if (password !== passwordConfirm) { showAlert('Пароли не совпадают', 'danger'); return; }
    if (password.length < 6) { showAlert('Пароль должен быть минимум 6 символов', 'danger'); return; }

    const { data, error } = await supabase.auth.signUp({
        email,
        password,
        options: { data: { firstName, lastName, bio: '' } }
    });

    if (error) {
        showAlert(error.message, 'danger');
        return;
    }

    // Supabase sends confirmation email depending on your project settings.
    showAlert('Регистрация выполнена. Подтвердите Email, если требуется.', 'success');
}

async function handleLogin(event) {
    event.preventDefault();
    const email = document.getElementById('loginEmail').value.trim().toLowerCase();
    const password = document.getElementById('loginPassword').value;

    const { data, error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) { showAlert(error.message, 'danger'); return; }

    currentUser = data.user;
    showAlert(`Добро пожаловать, ${currentUser.user_metadata?.firstName || currentUser.email}!`, 'success');
    showApp();
}

async function logout() {
    await supabase.auth.signOut();
    currentUser = null;
    showAuth();
    showAlert('Вы вышли из аккаунта', 'info');
}

// === Photos Management ===
async function handleUpload(event) {
    event.preventDefault();
    if (!currentUser) { showAlert('Сначала войдите в систему', 'warning'); return; }
    const files = document.getElementById('fileInput').files;
    if (files.length === 0) { showAlert('Выберите файлы для загрузки', 'warning'); return; }

    const uploaded = [];
    for (const file of Array.from(files)) {
        const ext = file.name.split('.').pop();
        const fileName = `${Date.now()}-${Math.random().toString(36).slice(2)}.${ext}`;
        const path = `${currentUser.id}/${fileName}`;

        const { error } = await supabase.storage.from('photos').upload(path, file, { cacheControl: '3600', upsert: false });
        if (error) {
            console.error('Upload error', error);
            showAlert(`Ошибка загрузки ${file.name}: ${error.message}`, 'danger');
            continue;
        }

        const { data: urlData } = supabase.storage.from('photos').getPublicUrl(path);
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
    const { data, error } = await supabase.storage.from('photos').list(currentUser.id, { limit: 100, sortBy: { column: 'name', order: 'desc' } });
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
        const { data: urlData } = supabase.storage.from('photos').getPublicUrl(path);
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
            const { error } = await supabase.storage.from('photos').remove([path]);
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
    const { data: { user }, error } = await supabase.auth.getUser();
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

    const { data, error } = await supabase.auth.updateUser({ data: { firstName, lastName, bio } });
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
