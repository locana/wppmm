#include "pch.h"
#include "CubeRenderer.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;

CubeRenderer::CubeRenderer()
{

}

void CubeRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();

	// Create DirectXTK objects
	/*
	auto device = m_d3dDevice.Get();
	auto context = m_d3dContext.Get();

	// m_font.reset( new SpriteFont( device, L"Assets\\italic.spritefont" ) );

	spriteBatch.reset( new SpriteBatch( context ) );

	*/

	/*
	DX::ThrowIfFailed(
	CreateDDSTextureFromFile( device, L"Assets\\windowslogo.dds", nullptr, m_texture1.ReleaseAndGetAddressOf() )
	);
	*/

	screen_buf_size = 0;
	debugValue = 0;
	hResult = 0;
	debugCount = 0;
	flag = false;
	m_loadingComplete = false;

	Direct3DBase::CreateDeviceResources();
	D3D11_BLEND_DESC blendDesc;
	ZeroMemory( &blendDesc, sizeof(blendDesc) );

	D3D11_RENDER_TARGET_BLEND_DESC rtbd;
	ZeroMemory( &rtbd, sizeof(rtbd) );


	rtbd.BlendEnable = TRUE;
	rtbd.SrcBlend = D3D11_BLEND_SRC_ALPHA;
	rtbd.DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	rtbd.BlendOp = D3D11_BLEND_OP_ADD;
	rtbd.SrcBlendAlpha = D3D11_BLEND_ONE;
	rtbd.DestBlendAlpha = D3D11_BLEND_ZERO;
	rtbd.BlendOpAlpha = D3D11_BLEND_OP_ADD;
	rtbd.RenderTargetWriteMask = 0x0f;



	blendDesc.AlphaToCoverageEnable = false;
	blendDesc.RenderTarget[0] = rtbd;

	m_d3dDevice->CreateBlendState(&blendDesc, &Transparency);


	D3D11_RASTERIZER_DESC cmdesc;
	ZeroMemory(&cmdesc, sizeof(D3D11_RASTERIZER_DESC));

	cmdesc.FillMode = D3D11_FILL_SOLID;
	cmdesc.CullMode = D3D11_CULL_BACK;
	cmdesc.DepthClipEnable = TRUE;


	cmdesc.FrontCounterClockwise = true;
	m_d3dDevice->CreateRasterizerState(&cmdesc, &CCWcullMode);

	cmdesc.FrontCounterClockwise = false;
	m_d3dDevice->CreateRasterizerState(&cmdesc, &CWcullMode);

	

	auto loadVSTask = DX::ReadDataAsync("SimpleVertexShader.cso");
	auto loadPSTask = DX::ReadDataAsync("SimplePixelShader.cso");

	auto createVSTask = loadVSTask.then([this](Platform::Array<byte>^ fileData) {
				
		DX::ThrowIfFailed(
			m_d3dDevice->CreateVertexShader(
			fileData->Data,
			fileData->Length,
			nullptr,
			&m_vertexShader
			)
			);

		
		const D3D11_INPUT_ELEMENT_DESC vertexDesc[] = 
		{
			{ "POSITION",   0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0,  D3D11_INPUT_PER_VERTEX_DATA, 0 },
			{ "TEXCOORD",    0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		};
		

		// ここで死ぬ　E_INVALIDARG　が帰ってくるぽい
	//	DX::ThrowIfFailed(
		m_d3dDevice->CreateInputLayout(
			vertexDesc,
			ARRAYSIZE(vertexDesc),
			fileData->Data,
			fileData->Length,
			&m_inputLayout
			)
		//	)
		;
	});

	auto createPSTask = loadPSTask.then([this](Platform::Array<byte>^ fileData) {
		
		DX::ThrowIfFailed(
			m_d3dDevice->CreatePixelShader(
			fileData->Data,
			fileData->Length,
			nullptr,
			&m_pixelShader
			)
			);

		CD3D11_BUFFER_DESC constantBufferDesc(sizeof(ModelViewProjectionConstantBuffer), D3D11_BIND_CONSTANT_BUFFER);
		DX::ThrowIfFailed(
			m_d3dDevice->CreateBuffer(
			&constantBufferDesc,
			nullptr,
			&m_constantBuffer
			)
			);
	});

	auto createCubeTask = (createPSTask && createVSTask).then([this] () {
		Vertex v[] =
		{
			// Front Face
			Vertex(-1.0f, -1.0f, -1.0f, 0.0f, 1.0f),
			Vertex(-1.0f,  1.0f, -1.0f, 0.0f, 0.0f),
			Vertex( 1.0f,  1.0f, -1.0f, 1.0f, 0.0f),
			Vertex( 1.0f, -1.0f, -1.0f, 1.0f, 1.0f),

			// Back Face
			Vertex(-1.0f, -1.0f, 1.0f, 1.0f, 1.0f),
			Vertex( 1.0f, -1.0f, 1.0f, 0.0f, 1.0f),
			Vertex( 1.0f,  1.0f, 1.0f, 0.0f, 0.0f),
			Vertex(-1.0f,  1.0f, 1.0f, 1.0f, 0.0f),

			// Top Face
			Vertex(-1.0f, 1.0f, -1.0f, 0.0f, 1.0f),
			Vertex(-1.0f, 1.0f,  1.0f, 0.0f, 0.0f),
			Vertex( 1.0f, 1.0f,  1.0f, 1.0f, 0.0f),
			Vertex( 1.0f, 1.0f, -1.0f, 1.0f, 1.0f),

			// Bottom Face
			Vertex(-1.0f, -1.0f, -1.0f, 1.0f, 1.0f),
			Vertex( 1.0f, -1.0f, -1.0f, 0.0f, 1.0f),
			Vertex( 1.0f, -1.0f,  1.0f, 0.0f, 0.0f),
			Vertex(-1.0f, -1.0f,  1.0f, 1.0f, 0.0f),

			// Left Face
			Vertex(-1.0f, -1.0f,  1.0f, 0.0f, 1.0f),
			Vertex(-1.0f,  1.0f,  1.0f, 0.0f, 0.0f),
			Vertex(-1.0f,  1.0f, -1.0f, 1.0f, 0.0f),
			Vertex(-1.0f, -1.0f, -1.0f, 1.0f, 1.0f),

			// Right Face
			Vertex( 1.0f, -1.0f, -1.0f, 0.0f, 1.0f),
			Vertex( 1.0f,  1.0f, -1.0f, 0.0f, 0.0f),
			Vertex( 1.0f,  1.0f,  1.0f, 1.0f, 0.0f),
			Vertex( 1.0f, -1.0f,  1.0f, 1.0f, 1.0f),
		};



		D3D11_SUBRESOURCE_DATA vertexBufferData = {0};
		vertexBufferData.pSysMem = v;
		vertexBufferData.SysMemPitch = 0;
		vertexBufferData.SysMemSlicePitch = 0;
		CD3D11_BUFFER_DESC vertexBufferDesc(sizeof(v), D3D11_BIND_VERTEX_BUFFER);
		DX::ThrowIfFailed(
			m_d3dDevice->CreateBuffer(
			&vertexBufferDesc,
			&vertexBufferData,
			&m_vertexBuffer
			)
			);

		DWORD indices[] = {
			// Front Face
			0,  2,  1,
			0,  3,  2,

			// Back Face
			4,  6,  5,
			4,  7,  6,

			// Top Face
			8,  10, 9,
			8, 11, 10,

			// Bottom Face
			12, 14, 13,
			12, 15, 14,

			// Left Face
			16, 18, 17,
			16, 19, 18,

			// Right Face
			20, 22, 21,
			20, 23, 22
		};

		m_indexCount = ARRAYSIZE(indices);

		D3D11_SUBRESOURCE_DATA indexBufferData = {0};
		indexBufferData.pSysMem = indices;
		indexBufferData.SysMemPitch = 0;
		indexBufferData.SysMemSlicePitch = 0;
		CD3D11_BUFFER_DESC indexBufferDesc(sizeof(indices), D3D11_BIND_INDEX_BUFFER);
		DX::ThrowIfFailed(
			m_d3dDevice->CreateBuffer(
			&indexBufferDesc,
			&indexBufferData,
			&m_indexBuffer
			)
			);
	});

	createCubeTask.then([this] () {
		m_loadingComplete = true;
	});



}

void CubeRenderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();

	float aspectRatio = m_windowBounds.Width / m_windowBounds.Height;
	float fovAngleY = 70.0f * XM_PI / 180.0f;
	if (aspectRatio < 1.0f)
	{
		fovAngleY /= aspectRatio;
	}

	XMStoreFloat4x4(
		&m_constantBufferData.projection,
		XMMatrixTranspose(
		XMMatrixPerspectiveFovRH(
		fovAngleY,
		aspectRatio,
		0.01f,
		100.0f
		)
		)
		);
}

void CubeRenderer::Update(float timeTotal, float timeDelta)
{
	(void) timeDelta; // Unused parameter.

	XMVECTOR eye = XMVectorSet(0.0f, 0.0f, 3.f, 0.0f);
	XMVECTOR at = XMVectorSet(0.0f, 0.0f, 0.0f, 0.0f);
	XMVECTOR up = XMVectorSet(0.0f, 1.0f, 0.0f, 0.0f);

	XMStoreFloat4x4(&m_constantBufferData.view, XMMatrixTranspose(XMMatrixLookAtRH(eye, at, up)));
	XMStoreFloat4x4(&m_constantBufferData.model, XMMatrixTranspose(XMMatrixRotationY(timeTotal * XM_PIDIV4)));


}

void CubeRenderer::Render()
{    // clear
	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	const float midnightBlue[] = { 0.098f, 0.098f, 0.439f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		midnightBlue
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);

	if (!SRV || !flag || !m_loadingComplete){
		return;
	}

	debugCount++;


	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	m_d3dContext->UpdateSubresource(
		m_constantBuffer.Get(),
		0,
		NULL,
		&m_constantBufferData,
		0,
		0
		);

	UINT stride = sizeof(Vertex);
	UINT offset = 0;
	m_d3dContext->IASetVertexBuffers(
		0,
		1,
		m_vertexBuffer.GetAddressOf(),
		&stride,
		&offset
		);

	m_d3dContext->IASetIndexBuffer(
		m_indexBuffer.Get(),
		DXGI_FORMAT_R32_UINT,
		0
		);


	m_d3dContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

	m_d3dContext->IASetInputLayout(m_inputLayout.Get());

	m_d3dContext->VSSetShader(
		m_vertexShader.Get(),
		nullptr,
		0
		);

	m_d3dContext->VSSetConstantBuffers(
		0,
		1,
		m_constantBuffer.GetAddressOf()
		);

	m_d3dContext->PSSetShader(
		m_pixelShader.Get(),
		nullptr,
		0
		);

	m_d3dContext->PSSetShaderResources( 0, 1, SRV.GetAddressOf());
	m_d3dContext->PSSetSamplers( 0, 1, &CubesTexSamplerState );

	float blendFactor[] = {0.75f, 0.75f, 0.75f, 1.0f};
	m_d3dContext->OMSetBlendState(Transparency.Get(), blendFactor, 0xffffffff);

	m_d3dContext->RSSetState(CCWcullMode.Get());
	m_d3dContext->DrawIndexed(
		m_indexCount,
		0,
		0
		);

	m_d3dContext->RSSetState(CWcullMode.Get());
	m_d3dContext->DrawIndexed(
		m_indexCount,
		0,
		0
		);

	/*
	if(SUCCEEDED(result))
	{
	// spriteBatch->Draw( m_texture1.Get(), XMFLOAT2(10,75), nullptr, Colors::White );
	m_d3dContext->PSSetShaderResources( 0, 1, SRV.GetAddressOf());
	m_d3dContext->PSSetSamplers( 0, 1, &CubesTexSamplerState );

	//float blendFactor[] = {0.75f, 0.75f, 0.75f, 1.0f};
	m_d3dContext->OMSetBlendState(Transparency.Get(), nullptr, 0xffffffff);

	m_d3dContext->RSSetState(CCWcullMode.Get());
	m_d3dContext->DrawIndexed(
	m_indexCount,
	0,
	0
	);

	m_d3dContext->RSSetState(CWcullMode.Get());
	m_d3dContext->DrawIndexed(
	m_indexCount,
	0,
	0
	);

	}else{
	CreateDDSTextureFromFile( m_d3dDevice.Get(), L"Assets\\windowslogo.dds", nullptr, m_texture1.ReleaseAndGetAddressOf() );
	spriteBatch->Draw( m_texture1.Get(), XMFLOAT2(10,75), nullptr, Colors::White );
	}

	*/



	/*
	wchar_t wStr[30];
	_ltow_s(result, wStr, 20);
	// m_font->DrawString( spriteBatch.get(), wStr , XMFLOAT2( 10, 10 ), Colors::Yellow );

	_itow_s(screen_buf_size, wStr, 10);
	// m_font->DrawString( spriteBatch.get(), wStr , XMFLOAT2( 310, 200 ), Colors::Yellow );
	*/

	// spriteBatch->End();
}

void CubeRenderer::setScreenData(const Platform::Array<byte>^ data){

	screen_buf_size = static_cast<int>( data->Length);

	if (SCREEN_BUF_MAX < screen_buf_size){
		return;
	}

	for(int i = 0 ; i < screen_buf_size; i++)
	{
		screen_buf[i] = data[i];
	}
}

int CubeRenderer::GetDebugValue(){
	return debugValue;
}

void CubeRenderer::setTexture(int* buffer, int width, int height){

	if(buffer)
	{

		//use uint32 buffer
		uint32 * uBuffer = (uint32 *)buffer;
		//compenstae alpha 
		std::vector<uint32> ARGBBuffer(width*height);
		//for each pixel

		for (int i =0; i <width*height;++i)
		{
			//extract alpha value
			uint8 a = uBuffer[i] >>24;

			//alpha = 0   => can't compensate RGB value
			//alpha = 255 => premultiplied_ARGB == ARGB
			if(a ==0 || a ==255)
			{

				ARGBBuffer[i] = uBuffer[i];
			}
			else
			{
				//compute alpha cefficient
				double aCoef = (uBuffer[i] >>24)/255.;

				//extract RGB value and compensate alpha coeficient
				uint8 r = (uBuffer[i] >>16 & 0xFF) /aCoef +.5;
				uint8 g = (uBuffer[i] >>8	& 0xFF) /aCoef +.5;
				uint8 b = (uBuffer[i]		& 0xFF) /aCoef +.5;

				//recreate ARGB value to uint32
				ARGBBuffer[i] = (a <<24) + (r <<16) + (g <<8) + b;


			}
		}



		CD3D11_TEXTURE2D_DESC textureDesc(
			DXGI_FORMAT_B8G8R8A8_UNORM,
			static_cast<UINT>(width),
			static_cast<UINT>(height),
			1,
			1,
			D3D11_BIND_SHADER_RESOURCE
			);
		int pixelSize = sizeof(int); 
		D3D11_SUBRESOURCE_DATA data;
		data.pSysMem = ARGBBuffer.data();
		data.SysMemPitch = pixelSize*width;
		data.SysMemSlicePitch =	pixelSize*width*height ;

		HREFTYPE hr;
		DX::ThrowIfFailed(
			hr = m_d3dDevice->CreateTexture2D(
			&textureDesc,
			&data,
			&m_texture2d
			)
			);


		hr = m_d3dDevice->CreateShaderResourceView(m_texture2d.Get(),NULL,&SRV); 



		D3D11_SAMPLER_DESC sampDesc;
		ZeroMemory( &sampDesc, sizeof(sampDesc) );
		sampDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
		sampDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
		sampDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
		sampDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
		sampDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
		sampDesc.MinLOD = 0;
		sampDesc.MaxLOD = D3D11_FLOAT32_MAX;
		m_d3dDevice->CreateSamplerState( &sampDesc, &CubesTexSamplerState );

		// comp data prepare.
		if(SUCCEEDED(hr)){
			flag = true;
		}
	}

}

int CubeRenderer::ConvertErrorCode(HRESULT hr){

	switch(hr){
	case D3D11_ERROR_FILE_NOT_FOUND:
		return 0;
		break;
	case D3D11_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS:
		return 1;
		break;
	case D3D11_ERROR_TOO_MANY_UNIQUE_VIEW_OBJECTS:
		return 2;
		break;
	case D3D11_ERROR_DEFERRED_CONTEXT_MAP_WITHOUT_INITIAL_DISCARD:
		return 3;
		break;
	case E_FAIL:
		return 4;
		break;
	case E_INVALIDARG:
		return 5;
		break;
	case E_OUTOFMEMORY:
		return 6;
		break;
	case S_FALSE:
		return 7;
		break;
	case S_OK:
		return 8;
		break;
	default:
		return 100;

	}
}

