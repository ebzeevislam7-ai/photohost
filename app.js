// === App State ===
let currentUser = null;

// === Initialization ===
document.addEventListener('DOMContentLoaded', function() {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
        currentUser = JSON.parse(savedUser);
        showApp();
    } else {
        showAuth();
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
    document.getElementById('authContainer').style.display = 'block';
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
function handleRegister(event) {
    event.preventDefault();

    const firstName = document.getElementById('regFirstName').value.trim();
    const lastName = document.getElementById('regLastName').value.trim();
    const email = document.getElementById('regEmail').value.trim().toLowerCase();
    const password = document.getElementById('regPassword').value;
    const passwordConfirm = document.getElementById('regPasswordConfirm').value;

    // Validation
    if (!firstName || !lastName || !email || !password) {
        showAlert('Заполните все поля', 'danger');
        return;
    }

    if (password !== passwordConfirm) {
        showAlert('Пароли не совпадают', 'danger');
        return;
    }

    if (password.length < 6) {
        showAlert('Пароль должен быть минимум 6 символов', 'danger');
        return;
    }

    // Check if user already exists
    const users = JSON.parse(localStorage.getItem('users') || '[]');
    if (users.some(u => u.email === email)) {
        showAlert('Пользователь с таким Email уже существует', 'danger');
        return;
    }

    // Create new user
    const newUser = {
        id: Date.now().toString(),
        email,
        password,
        firstName,
        lastName,
        bio: '',
        createdAt: new Date().toISOString()
    };

    users.push(newUser);
    localStorage.setItem('users', JSON.stringify(users));

    // Auto-login
    currentUser = newUser;
    localStorage.setItem('currentUser', JSON.stringify(currentUser));

    showApp();
    showAlert('Регистрация успешна! Добро пожаловать!', 'success');
}

function handleLogin(event) {
    event.preventDefault();

    const email = document.getElementById('loginEmail').value.trim().toLowerCase();
    const password = document.getElementById('loginPassword').value;

    const users = JSON.parse(localStorage.getItem('users') || '[]');
    const user = users.find(u => u.email === email && u.password === password);

    if (!user) {
        showAlert('Неверный Email или пароль', 'danger');
        return;
    }

    currentUser = user;
    localStorage.setItem('currentUser', JSON.stringify(currentUser));

    showApp();
    showAlert(`Добро пожаловать, ${user.firstName}!`, 'success');
}

function logout() {
    currentUser = null;
    localStorage.removeItem('currentUser');
    document.getElementById('loginEmail').value = '';
    document.getElementById('loginPassword').value = '';
    document.getElementById('regFirstName').value = '';
    document.getElementById('regLastName').value = '';
    document.getElementById('regEmail').value = '';
    document.getElementById('regPassword').value = '';
    document.getElementById('regPasswordConfirm').value = '';
    document.getElementById('preview').innerHTML = '';
    document.getElementById('fileInput').value = '';
    
    showAuth();
    showAlert('Вы вышли из аккаунта', 'info');
}

// === Photos Management ===
function handleUpload(event) {
    event.preventDefault();

    const files = document.getElementById('fileInput').files;
    if (files.length === 0) {
        showAlert('Выберите файлы для загрузки', 'warning');
        return;
    }

    const userPhotos = getUserPhotos();
    let uploadedCount = 0;

    Array.from(files).forEach(file => {
        const reader = new FileReader();
        reader.onload = function(e) {
            const photo = {
                id: Date.now().toString() + Math.random(),
                userId: currentUser.id,
                fileName: file.name,
                data: e.target.result, // base64 data URL
                uploadedAt: new Date().toISOString()
            };

            userPhotos.push(photo);
            uploadedCount++;

            if (uploadedCount === files.length) {
                localStorage.setItem(`photos_${currentUser.id}`, JSON.stringify(userPhotos));
                document.getElementById('fileInput').value = '';
                document.getElementById('preview').innerHTML = '';
                loadPhotos();
                showAlert(`Успешно загружено ${uploadedCount} фото!`, 'success');
            }
        };
        reader.readAsDataURL(file);
    });
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

function getUserPhotos() {
    return JSON.parse(localStorage.getItem(`photos_${currentUser.id}`) || '[]');
}

function loadPhotos() {
    const photos = getUserPhotos();
    const grid = document.getElementById('photoGrid');
    
    document.getElementById('photoCount').textContent = photos.length;

    if (photos.length === 0) {
        grid.innerHTML = '<div class="col-12"><div class="alert alert-light text-center">Нет загруженных фотографий. Начните с загрузки первой!</div></div>';
        return;
    }

    grid.innerHTML = photos.map(photo => `
        <div class="col">
            <div class="card h-100 shadow-sm">
                <img src="${photo.data}" class="card-img-top" alt="${photo.fileName}" style="cursor:pointer;" data-bs-toggle="modal" data-bs-target="#imageModal" onclick="showImage('${photo.data}', '${photo.fileName}')">
                <div class="card-body">
                    <h6 class="card-title text-truncate" title="${photo.fileName}">${photo.fileName}</h6>
                    <p class="text-muted mb-2 small">${new Date(photo.uploadedAt).toLocaleDateString('ru-RU', {year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit'})}</p>
                    <button class="btn btn-sm btn-outline-danger w-100" onclick="deletePhoto('${photo.id}')">Удалить</button>
                </div>
            </div>
        </div>
    `).join('');
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
function loadProfileData() {
    document.getElementById('profileEmail').value = currentUser.email;
    document.getElementById('profileFirstName').value = currentUser.firstName || '';
    document.getElementById('profileLastName').value = currentUser.lastName || '';
    document.getElementById('profileBio').value = currentUser.bio || '';
}

function saveProfile() {
    currentUser.firstName = document.getElementById('profileFirstName').value.trim();
    currentUser.lastName = document.getElementById('profileLastName').value.trim();
    currentUser.bio = document.getElementById('profileBio').value.trim();

    // Update users list
    const users = JSON.parse(localStorage.getItem('users') || '[]');
    const userIndex = users.findIndex(u => u.id === currentUser.id);
    if (userIndex !== -1) {
        users[userIndex] = currentUser;
        localStorage.setItem('users', JSON.stringify(users));
    }

    localStorage.setItem('currentUser', JSON.stringify(currentUser));
    
    const userName = currentUser.firstName || currentUser.email;
    document.getElementById('navUserName').textContent = userName;

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
