// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Shows notification
let localUrl = window.location.href;

// Find the index of the third '/' (after protocol and domain)
let parts = localUrl.split('/');
let baseURL = '';
if (parts.length > 3) {
    // Rebuild the URL while removing everything between the third '/' and 'api'
    baseURL = parts[0] + '//' + parts[2] + '/';
}

function showNotification(status, message) {

    // Create a new notification element
    const notification = document.createElement('div');
    notification.classList.add('notification'); // Add base notification class
    notification.textContent = message;

    // Determine the notification type based on status code
    if (status >= 100 && status < 200) {
        notification.classList.add('info');
    } else if (status >= 200 && status < 300) {
        notification.classList.add('success');
    } else if (status >= 400 && status < 500) {
        notification.classList.add('warning');
    } else if (status >= 500) {
        notification.classList.add('error');
    }

    // Append the notification to the document body
    document.body.appendChild(notification);

    // Adjust position for stacking
    const notifications = document.querySelectorAll('.notification');
    notifications.forEach((notif, index) => {
        notif.style.bottom = `${110 + index * 60}px`; // Adjust stacking gap
    });

    // Remove the notification after 3 seconds
    setTimeout(() => {
        notification.classList.remove('info', 'success', 'warning', 'error', 'show');
        setTimeout(() => {
            notification.remove(); // Remove the element after fade-out
        }, 500); // Match CSS transition duration
    }, 3000);    
}

async function postToController(url, data) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'accept': '*/*',
                'Authorization': `Bearer ${accessToken}`
            },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            const result = await response.json(); // Если метод возвращает JSON
            console.log('Successful responce:', result);
            return result;
        } else {
            const result = await response.json(); // Если метод возвращает JSON
            console.error('Error while execution a request:', result);
            return result;            
        }
    } catch (error) {
        console.error('Connection error:', error);
    }
}

async function getTemplates(url, selectTemplate, payload) {

    await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json', // Specify JSON content type
            'accept': '*/*',
            'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify(payload), // Convert the payload to a JSON string
        
    })
    .then(responce => {
        if (!responce.ok) {
            throw new Error('Network response was not ok');
        }
        return responce.json();
    })
    .then(data => {
        // Clean existing options
        const select = document.getElementById(selectTemplate);
        select.innerHTML = ''; // Clear existing options

        if (select) {
            select.innerHTML = ''; // Clear existing options

            const defaultOption = document.createElement('option');

            defaultOption.place
            defaultOption.value = '';
            defaultOption.textContent = 'Select item';
            select.appendChild(defaultOption);

            // Extract the array from the "value" property
            if (data && Array.isArray(data.value)) {
                data.value.forEach(item => {
                    const option = document.createElement('option');
                    option.value = item.value;
                    option.textContent = item.text;
                    select.appendChild(option);
                });
            } else {
                console.error('Response does not contain a valid "value" array:', data);
            }
        } else {
            console.error(`Select element with ID '${selectTemplate}' not found.`);
        }
    })
    .catch(error => {
        // Handle errors
        const select = document.getElementById(selectTemplate);
        // Ensure the select element exists
        if (select) {
            select.innerHTML = ''; // Clear existing options

            // Add an error message option
            const errorOption = document.createElement('option');
            errorOption.value = '';
            errorOption.textContent = 'Error while downloading data';
            select.appendChild(errorOption);
        } else {
            console.error(`Select element with ID '${selectTemplate}' not found.`);
        }

        console.error('There was a problem with the fetch operation:', error);
    });    
}

async function CreateProvisionBlock(baseURL, data) {
    // Arrays containing data for checkboxes
    
    const proxmoxHostItems = await GetProxmoxHosts(baseURL, data);//['Option 1', 'Option 2', 'Option 3'];
    const proxmoxStorageItems = await GetProxmoxStorages(baseURL, data); //['Option 1', 'Option 2', 'Option 3'];

    function createCheckboxBlock(items, blockId) {
        const block = document.createElement('div');
        block.id = blockId;
        const heading = document.createElement('h2');
        if (blockId === 'master') {
            heading.textContent = 'ETCDs Provision';
        }
        else if (blockId === 'worker') {
            heading.textContent = 'Worker Provision';
        }
        else {
            heading.textContent = 'VM Storage Allocation';
        }
        
        block.appendChild(heading);

        items.forEach(item => {
            const label = document.createElement('label');
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.name = blockId;
            checkbox.value = item;
            label.appendChild(checkbox);
            label.appendChild(document.createTextNode(item));
            block.appendChild(label);
            block.appendChild(document.createElement('br'));
        });

        return block;
    }

    // Adding the blocks to the page
    const container = document.getElementById('vmProvisionRange');
    container.innerHTML = '';
    container.appendChild(createCheckboxBlock(proxmoxHostItems, 'master'));
    container.appendChild(createCheckboxBlock(proxmoxHostItems, 'worker'));
    container.appendChild(createCheckboxBlock(proxmoxStorageItems, 'storage'));
}

// Function to get selected checkboxes
function GetSelectedCheckboxes(boxid) {
    const checkboxes = document.querySelectorAll('#' + boxid + ' input[type="checkbox"]:checked');
    const selectedValues = Array.from(checkboxes).map(checkbox => checkbox.value);

    return selectedValues;
}

async function GetProxmoxHosts(baseURL, data) {
    let hostURL = baseURL + 'api/Proxmox/GetProxmoxHosts';
    let proxmoxhosts = await postToController(hostURL, data);

    return proxmoxhosts.value;
}

async function GetProxmoxStorages(baseURL, data) {
    let hostURl = baseURL + 'api/Proxmox/GetProxmoxStorages'
    let proxmoxStorages = await postToController(hostURl, data);

    return proxmoxStorages.value;
}

async function GetVLANTags(baseURL, data) {
    let hostURl = baseURL + 'api/Proxmox/GetProxmoxVLANTags'
    let proxmoxVLANTags = await postToController(hostURl, data);

    return proxmoxVLANTags.value;
}
async function wait(seconds) {
    return new Promise(resolve => setTimeout(resolve, seconds * 1000));
}